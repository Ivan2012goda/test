import asyncio
import json
import sqlite3
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable


@dataclass(frozen=True)
class ScheduledMessage:
    chat_id: int
    text: str
    send_at: datetime


class MessageLogger:
    def __init__(self, db_path: Path) -> None:
        self.db_path = db_path
        self.db_path.parent.mkdir(parents=True, exist_ok=True)
        self._init_db()

    def _init_db(self) -> None:
        with sqlite3.connect(self.db_path) as conn:
            conn.execute(
                """
                CREATE TABLE IF NOT EXISTS messages (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    chat_id INTEGER NOT NULL,
                    sender_id INTEGER NOT NULL,
                    message TEXT NOT NULL,
                    created_at TEXT NOT NULL
                )
                """
            )
            conn.commit()

    async def log_message(self, chat_id: int, sender_id: int, message: str) -> None:
        created_at = datetime.now(timezone.utc).isoformat()
        await asyncio.to_thread(self._write, chat_id, sender_id, message, created_at)

    def _write(self, chat_id: int, sender_id: int, message: str, created_at: str) -> None:
        with sqlite3.connect(self.db_path) as conn:
            conn.execute(
                "INSERT INTO messages (chat_id, sender_id, message, created_at) VALUES (?, ?, ?, ?)",
                (chat_id, sender_id, message, created_at),
            )
            conn.commit()

    async def count_messages(self) -> int:
        return await asyncio.to_thread(self._count)

    def _count(self) -> int:
        with sqlite3.connect(self.db_path) as conn:
            cursor = conn.execute("SELECT COUNT(*) FROM messages")
            (count,) = cursor.fetchone()
            return int(count)


class KeywordResponder:
    def __init__(self, rules_file: Path) -> None:
        self.rules_file = rules_file
        self.rules_file.parent.mkdir(parents=True, exist_ok=True)
        if not self.rules_file.exists():
            self._write_default_rules()

    def _write_default_rules(self) -> None:
        default_rules = {
            "rules": [
                {"keyword": "привет", "reply": "Привет! Чем могу помочь?"},
                {"keyword": "прайс", "reply": "Прайс-лист можно запросить у менеджера."},
            ]
        }
        self.rules_file.write_text(json.dumps(default_rules, ensure_ascii=False, indent=2), encoding="utf-8")

    def load_rules(self) -> list[dict[str, str]]:
        data = json.loads(self.rules_file.read_text(encoding="utf-8"))
        return data.get("rules", [])

    def save_rules(self, rules: list[dict[str, str]]) -> None:
        payload = {"rules": rules}
        self.rules_file.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")

    def add_rule(self, keyword: str, reply: str) -> None:
        rules = self.load_rules()
        rules.append({"keyword": keyword, "reply": reply})
        self.save_rules(rules)

    def match_reply(self, text: str) -> str | None:
        lowered = text.lower()
        for rule in self.load_rules():
            keyword = rule.get("keyword", "").lower()
            reply = rule.get("reply", "")
            if keyword and keyword in lowered:
                return reply
        return None


class Scheduler:
    def __init__(self, schedule_file: Path) -> None:
        self.schedule_file = schedule_file
        self.schedule_file.parent.mkdir(parents=True, exist_ok=True)
        if not self.schedule_file.exists():
            self.schedule_file.write_text("[]", encoding="utf-8")
        self._lock = asyncio.Lock()

    async def list_jobs(self) -> list[ScheduledMessage]:
        async with self._lock:
            return await asyncio.to_thread(self._read_jobs)

    def _read_jobs(self) -> list[ScheduledMessage]:
        raw = json.loads(self.schedule_file.read_text(encoding="utf-8"))
        jobs: list[ScheduledMessage] = []
        for item in raw:
            jobs.append(
                ScheduledMessage(
                    chat_id=int(item["chat_id"]),
                    text=item["text"],
                    send_at=datetime.fromisoformat(item["send_at"]),
                )
            )
        return jobs

    async def save_jobs(self, jobs: Iterable[ScheduledMessage]) -> None:
        async with self._lock:
            await asyncio.to_thread(self._write_jobs, list(jobs))

    def _write_jobs(self, jobs: list[ScheduledMessage]) -> None:
        payload = [
            {"chat_id": job.chat_id, "text": job.text, "send_at": job.send_at.isoformat()}
            for job in jobs
        ]
        self.schedule_file.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
