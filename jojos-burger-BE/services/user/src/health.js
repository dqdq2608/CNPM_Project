export default async function health(fastify) {
    fastify.get("/health", async () => ({ status: "ok", service: process.env.SERVICE_NAME }));
  }
  