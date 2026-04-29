import os
import psycopg2
from psycopg2.extras import RealDictCursor
from pgvector.psycopg2 import register_vector
from dotenv import load_dotenv

load_dotenv()

DB_URL = os.getenv("DATABASE_URL")
if not DB_URL:
    raise RuntimeError(
        "DATABASE_URL environment variable not set. "
        "The Python microservice expects the same Postgres connection "
        "string as the .NET API."
    )


def get_connection(dict_cursor: bool = False):
    """
    Open a Postgres connection with pgvector support enabled.

    Args:
        dict_cursor: if True, queries return dicts keyed by column name
            instead of positional tuples. Useful for the API layer where
            named access is clearer; not necessary for bulk ETL scripts.

    Returns:
        psycopg2 connection. Caller is responsible for committing,
        closing, etc. — typically wrap in `with` or try/finally.
    """
    conn_kwargs = {}
    if dict_cursor:
        conn_kwargs["cursor_factory"] = RealDictCursor

    conn = psycopg2.connect(DB_URL, **conn_kwargs)

    # register_vector teaches psycopg2 how to convert between numpy arrays
    # / lists and pgvector's `vector` Postgres type.
    register_vector(conn)
    return conn


def ensure_extension():
    """
    Safety-net helper to make sure pgvector is enabled. Normally the EF
    migration handles this; useful when running the Python pipeline
    against a fresh DB or in tests.
    """
    with get_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("CREATE EXTENSION IF NOT EXISTS vector;")
        conn.commit()