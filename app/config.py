from dataclasses import dataclass
from pathlib import Path

from dotenv import load_dotenv
import os


@dataclass(frozen=True)
class AppConfig:
    api_id: int
    api_hash: str
    session_name: str
    log_db_path: Path
    schedule_file: Path
    keyword_rules_file: Path


def load_config() -> AppConfig:
    load_dotenv()
    api_id = int(os.environ["API_ID"])
    api_hash = os.environ["API_HASH"]
    session_name = os.environ.get("SESSION_NAME", "feature_client")
    log_db_path = Path(os.environ.get("LOG_DB_PATH", "data/messages.db"))
    schedule_file = Path(os.environ.get("SCHEDULE_FILE", "data/schedule.json"))
    keyword_rules_file = Path(os.environ.get("KEYWORD_RULES_FILE", "data/keyword_rules.json"))

    return AppConfig(
        api_id=api_id,
        api_hash=api_hash,
        session_name=session_name,
        log_db_path=log_db_path,
        schedule_file=schedule_file,
        keyword_rules_file=keyword_rules_file,
    )
