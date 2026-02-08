# Telegram client with "extras"

This is a Python Telegram client powered by [Telethon](https://github.com/LonamiWebs/Telethon) and a few extra features:

- **Message logging** to SQLite for quick history lookup.
- **Keyword auto-replies** for simple support/FAQ flows.
- **Scheduled messages** with a lightweight scheduler.
- **/stats command** to show basic local stats.

## Setup

1. Create a Telegram application and obtain your API ID and API HASH:
   - https://my.telegram.org/apps
2. Copy the example environment file:

```bash
cp .env.example .env
```

3. Fill in `.env` with your credentials.
4. Install dependencies:

```bash
pip install -r requirements.txt
```

## Run

```bash
python -m app.main
```

## Web UI

После запуска клиента интерфейс доступен на `http://localhost:8000`.

Для локального просмотра интерфейса без запуска Telegram-клиента (API_ID и API_HASH не требуются):

```bash
python -m app.ui
```

## Notes

- The first run will ask you to log in and create a session file.
- Logging data is stored in `data/messages.db`.
