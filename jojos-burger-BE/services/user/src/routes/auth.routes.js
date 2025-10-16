import { SignupSchema, LoginSchema } from "../schemas/auth.schema.js";
import { signupHandler, loginHandler, meHandler } from "../controllers/auth.controller.js";

export default async function routes(fastify) {
  fastify.post("/auth/signup", {
    schema: { body: SignupSchema.strict() },
    handler: signupHandler
  });

  fastify.post("/auth/login", {
    schema: { body: LoginSchema.strict() },
    handler: loginHandler
  });

  fastify.get("/me", { preHandler: [fastify.auth] }, meHandler);
}
