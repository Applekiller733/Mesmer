import os
import psycopg2
from psycopg2.extras import RealDictCursor
from pgvector.psycopg2 import register_vector
from dotenv import load_dotenv

load_dotenv()

# Two connection modes, mirroring the .NET backend (backend/Helpers/DbConnection.cs)
# so both services talk to the same Aurora cluster the same way:
#   - In AWS (DB_HOST set): Aurora over IAM authentication. A short-lived RDS auth
#     token is generated locally (no network call) and used as the password, over
#     TLS. No DB password is stored anywhere.
#   - Locally (no DB_HOST): the plain DATABASE_URL connection string.
DB_HOST = os.getenv("DB_HOST")
DB_URL = os.getenv("DATABASE_URL")

if not DB_HOST and not DB_URL:
    raise RuntimeError(
        "No database configuration found. Set DB_HOST (+ DB_PORT/DB_NAME/"
        "DB_USER) for Aurora IAM auth in AWS, or DATABASE_URL for a local "
        "connection string."
    )


def _iam_connection_kwargs() -> dict:
    # Build psycopg2 connect kwargs using a freshly generated RDS IAM auth token
    # as the password. boto3 is only imported here so local (DATABASE_URL) usage
    # doesn't require it. The token is valid ~15 min; psycopg2 only authenticates
    # at connect time, so generating one per connection is sufficient.
    import boto3

    host = DB_HOST
    port = int(os.getenv("DB_PORT", "5432"))
    dbname = os.getenv("DB_NAME", "postgres")
    user = os.getenv("DB_USER", "postgres")
    # Lambda sets AWS_REGION automatically; fall back to the stack's region.
    region = os.getenv("AWS_REGION") or os.getenv("AWS_DEFAULT_REGION") or "eu-north-1"

    rds = boto3.client("rds", region_name=region)
    token = rds.generate_db_auth_token(
        DBHostname=host, Port=port, DBUsername=user, Region=region
    )

    return {
        "host": host,
        "port": port,
        "dbname": dbname,
        "user": user,
        "password": token,
        "sslmode": "require",  # IAM auth requires TLS
        # Aurora Serverless v2 (min 0 ACU) pauses when idle; the first connection
        # has to resume it (~15-30s). Give the resume room, matching the backend.
        "connect_timeout": 60,
    }


def get_connection(dict_cursor: bool = False):

    conn_kwargs = {}
    if dict_cursor:
        conn_kwargs["cursor_factory"] = RealDictCursor

    if DB_HOST:
        conn = psycopg2.connect(**_iam_connection_kwargs(), **conn_kwargs)
    else:
        conn = psycopg2.connect(DB_URL, **conn_kwargs)

    register_vector(conn)
    return conn


# Module-level connection reused across warm Lambda invocations by the serving
# API. Lambda runs one request per execution environment at a time, so a single
# connection is safe. Batch scripts keep using get_connection().
_shared_conn = None

# Postgres closes the shared connection after this long idle. A frozen warm
# Lambda would otherwise hold it open indefinitely, and Aurora Serverless v2
# does not auto-pause while a connection is open.
_SHARED_IDLE_TIMEOUT_MS = 60000


def _open_shared_connection():
    options = f"-c idle_session_timeout={_SHARED_IDLE_TIMEOUT_MS}"
    if DB_HOST:
        conn = psycopg2.connect(**_iam_connection_kwargs(), options=options)
    else:
        conn = psycopg2.connect(DB_URL, options=options)
    register_vector(conn)
    return conn


def get_shared_connection():
    # The IAM token is only checked at connect time, so its ~15 min expiry does
    # not affect a connection that is already open.
    global _shared_conn
    if _shared_conn is None or _shared_conn.closed:
        _shared_conn = _open_shared_connection()
    return _shared_conn


def reset_shared_connection():
    # Drop a connection the server has closed (idle timeout, Aurora pause).
    # `.closed` does not detect a server-side disconnect, so callers use this
    # after catching an OperationalError or InterfaceError.
    global _shared_conn
    if _shared_conn is not None:
        try:
            _shared_conn.close()
        except Exception:
            pass
    _shared_conn = None


def ensure_extension():
    with get_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("CREATE EXTENSION IF NOT EXISTS vector;")
        conn.commit()
