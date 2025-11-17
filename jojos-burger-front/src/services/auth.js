import { bffPublicApi } from "./bffPublicApi";

export async function fetchCurrentUser() {
  await bffPublicApi.initAntiforgery();

  const dto = await bffPublicApi.getUser();
  if (!dto) return null;

  const entries = dto.raw || [];
  const claims = Object.fromEntries(entries.map((c) => [c.type, c.value]));

  const displayName =
    dto.name ||
    claims["name"] ||
    claims["email"] ||
    claims["sub"] ||
    "User";

  return { claims, displayName, dto };
}
