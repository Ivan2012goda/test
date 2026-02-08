import asyncio
from datetime import datetime, timedelta, timezone
from pathlib import Path

import uvicorn
from telethon import TelegramClient, events

from app.config import load_config
from app.features import KeywordResponder, MessageLogger, Scheduler, ScheduledMessage
from app.web import create_app


async def schedule_loop(client: TelegramClient, scheduler: Scheduler) -> None:
    while True:
        now = datetime.now(timezone.utc)
        jobs = await scheduler.list_jobs()
        remaining_jobs: list[ScheduledMessage] = []
        for job in jobs:
            if job.send_at <= now:
                await client.send_message(job.chat_id, job.text)
            else:
                remaining_jobs.append(job)
        if len(remaining_jobs) != len(jobs):
            await scheduler.save_jobs(remaining_jobs)
        await asyncio.sleep(5)


async def main() -> None:
    config = load_config()
    logger = MessageLogger(config.log_db_path)
    responder = KeywordResponder(config.keyword_rules_file)
    scheduler = Scheduler(config.schedule_file)

    client = TelegramClient(config.session_name, config.api_id, config.api_hash)
    base_dir = Path(__file__).resolve().parent
    web_app = create_app(
        logger=logger,
        responder=responder,
        scheduler=scheduler,
        templates_path=base_dir / "templates",
        static_path=base_dir / "static",
    )
    config_uvicorn = uvicorn.Config(web_app, host="0.0.0.0", port=8000, log_level="info")
    server = uvicorn.Server(config_uvicorn)

    @client.on(events.NewMessage())
    async def handler(event: events.NewMessage.Event) -> None:
        sender = await event.get_sender()
        sender_id = sender.id if sender else 0
        text = event.raw_text
        await logger.log_message(event.chat_id, sender_id, text)

        if text.startswith("/stats"):
            count = await logger.count_messages()
            await event.reply(f"Локально сохранено сообщений: {count}")
            return

        if text.startswith("/schedule"):
            parts = text.split(maxsplit=2)
            if len(parts) < 3:
                await event.reply("Формат: /schedule <minutes> <text>")
                return
            minutes = int(parts[1])
            send_at = datetime.now(timezone.utc) + timedelta(minutes=minutes)
            jobs = await scheduler.list_jobs()
            jobs.append(ScheduledMessage(chat_id=event.chat_id, text=parts[2], send_at=send_at))
            await scheduler.save_jobs(jobs)
            await event.reply(f"Запланировано на {send_at.strftime('%H:%M:%S UTC')}")
            return

        reply = responder.match_reply(text)
        if reply:
            await event.reply(reply)

    async with client:
        await client.start()
        await asyncio.gather(
            client.run_until_disconnected(),
            schedule_loop(client, scheduler),
            server.serve(),
        )


if __name__ == "__main__":
    asyncio.run(main())
