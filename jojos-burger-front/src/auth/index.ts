import { oidc } from "./oidc";
export async function login() {
  await oidc.signinRedirect();
}
export async function logout() {
  await oidc.signoutRedirect();
}
export async function handleCallback() {
  await oidc.signinRedirectCallback();
}
export async function getAccessToken() {
  const user = await oidc.getUser();
  return user?.access_token ?? "";
}
