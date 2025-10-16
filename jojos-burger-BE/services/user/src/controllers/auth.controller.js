import { prisma } from "../lib/prisma.js";
import { hash, compare } from "../lib/hash.js";

export async function signupHandler(req, reply) {
  const { email, password, name } = req.body;

  const exists = await prisma.user.findUnique({ where: { email } });
  if (exists) return reply.badRequest("Email already registered");

  const user = await prisma.user.create({
    data: {
      email,
      name,
      passwordHash: await hash(password),
      // role: 'USER' // không cần set vì đã default
    },
    select: { id: true, email: true, name: true, role: true, createdAt: true },
  });

  req.server.metrics.userSignup.inc();

  const token = await reply.jwtSign({
    sub: user.id,
    email: user.email,
    role: user.role,
  });
  return reply.code(201).send({ token, user });
}

export async function loginHandler(req, reply) {
  const { email, password } = req.body;
  const user = await prisma.user.findUnique({
    where: { email },
    select: {
      id: true,
      email: true,
      name: true,
      role: true,
      passwordHash: true,
    },
  });
  if (!user) return reply.unauthorized("Invalid credentials");

  const ok = await compare(password, user.passwordHash);
  if (!ok) return reply.unauthorized("Invalid credentials");

  req.server.metrics.userLogin.inc();

  const token = await reply.jwtSign({
    sub: user.id,
    email: user.email,
    role: user.role,
  });
  // bỏ passwordHash khỏi response
  const { passwordHash, ...safe } = user;
  return { token, user: safe };
}

export async function meHandler(req) {
  const { sub } = req.user;
  const user = await prisma.user.findUnique({
    where: { id: sub },
    select: { id: true, email: true, name: true, role: true, createdAt: true },
  });
  return { user };
}
