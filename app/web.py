from __future__ import annotations

from datetime import datetime, timedelta, timezone
from pathlib import Path

from fastapi import FastAPI, Form, Request
from fastapi.responses import RedirectResponse
from fastapi.staticfiles import StaticFiles
from fastapi.templating import Jinja2Templates

from app.features import KeywordResponder, MessageLogger, Scheduler, ScheduledMessage


def create_app(
    logger: MessageLogger,
    responder: KeywordResponder,
    scheduler: Scheduler,
    templates_path: Path,
    static_path: Path,
) -> FastAPI:
    app = FastAPI(title="Feature Telegram Client")
    templates = Jinja2Templates(directory=str(templates_path))
    app.mount("/static", StaticFiles(directory=str(static_path)), name="static")

    @app.get("/")
    async def dashboard(request: Request) -> object:
        message_count = await logger.count_messages()
        rules = responder.load_rules()
        jobs = await scheduler.list_jobs()
        jobs_sorted = sorted(jobs, key=lambda job: job.send_at)
        now = datetime.now(timezone.utc)
        return templates.TemplateResponse(
            "index.html",
            {
                "request": request,
                "message_count": message_count,
                "rules": rules,
                "jobs": jobs_sorted,
                "now": now,
            },
        )

    @app.post("/rules")
    async def add_rule(keyword: str = Form(...), reply: str = Form(...)) -> RedirectResponse:
        responder.add_rule(keyword.strip(), reply.strip())
        return RedirectResponse(url="/", status_code=303)

    @app.post("/schedule")
    async def add_schedule(
        chat_id: int = Form(...),
        minutes: int = Form(...),
        text: str = Form(...),
    ) -> RedirectResponse:
        send_at = datetime.now(timezone.utc) + timedelta(minutes=minutes)
        jobs = await scheduler.list_jobs()
        jobs.append(ScheduledMessage(chat_id=chat_id, text=text, send_at=send_at))
        await scheduler.save_jobs(jobs)
        return RedirectResponse(url="/", status_code=303)

    return app
