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

    conn_kwargs = {}
    if dict_cursor:
        conn_kwargs["cursor_factory"] = RealDictCursor

    conn = psycopg2.connect(DB_URL, **conn_kwargs)

    register_vector(conn)
    return conn


def ensure_extension():
    with get_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("CREATE EXTENSION IF NOT EXISTS vector;")
        conn.commit()