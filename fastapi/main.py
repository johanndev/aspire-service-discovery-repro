import fastapi
import fastapi.responses

app = fastapi.FastAPI()


@app.get("/testendpoint", response_class=fastapi.responses.PlainTextResponse)
async def testendpoint():
    return "Hello from fastapi"
