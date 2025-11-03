import { UserManager, WebStorageStateStore } from "oidc-client-ts";

// @ts-ignore
export const oidc = new UserManager({
  authority: import.meta.env.VITE_IDENTITY_AUTHORITY,
  client_id: import.meta.env.VITE_OIDC_CLIENT_ID,
  redirect_uri: import.meta.env.VITE_OIDC_REDIRECT_URI,
  post_logout_redirect_uri: import.meta.env.VITE_OIDC_POST_LOGOUT_REDIRECT_URI,
  response_type: "code",
  scope: "openid profile email burger.api",
  userStore: new WebStorageStateStore({ store: window.sessionStorage }),
});
