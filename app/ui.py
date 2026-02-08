import asyncio
from pathlib import Path

import uvicorn

from app.config import load_config
from app.features import KeywordResponder, MessageLogger, Scheduler
from app.web import create_app


async def run_ui() -> None:
    config = load_config(require_api=False)
    logger = MessageLogger(config.log_db_path)
    responder = KeywordResponder(config.keyword_rules_file)
    scheduler = Scheduler(config.schedule_file)

    base_dir = Path(__file__).resolve().parent
    app = create_app(
        logger=logger,
        responder=responder,
        scheduler=scheduler,
        templates_path=base_dir / "templates",
        static_path=base_dir / "static",
    )

    config_uvicorn = uvicorn.Config(app, host="0.0.0.0", port=8000, log_level="info")
    server = uvicorn.Server(config_uvicorn)
    await server.serve()


if __name__ == "__main__":
    asyncio.run(run_ui())
