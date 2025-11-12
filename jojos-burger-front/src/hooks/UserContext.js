import React, {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import PropTypes from "prop-types";
import api, { ensureCsrfToken } from "../services/api";

// read cookie
function readCookie(name) {
  const m = document.cookie.match(new RegExp(`(?:^|; )${name}=([^;]*)`));
  return m ? decodeURIComponent(m[1]) : null;
}

// /bff/public/user
async function fetchBffUser() {
  await ensureCsrfToken();
  const r = await api.get("/bff/public/user");
  const u = r.data;
  if (!u) throw new Error("No user");

  const rawArr = Array.isArray(u.raw) ? u.raw : [];
  const claim = Object.fromEntries(rawArr.map((c) => [c.type, c.value]));

  // get full name
  const full = [claim.given_name, claim.family_name]
    .filter(Boolean)
    .join(" ")
    .trim();

  // display name on header
  const displayName =
    claim.name ||
    (u.name && u.name !== u.email ? u.name : null) ||
    full ||
    claim.preferred_username ||
    claim.email ||
    u.email ||
    claim.sub ||
    u.sub ||
    "User";

  return {
    sub: u.sub ?? claim.sub ?? null,
    name: claim.name ?? u.name ?? null,
    email: claim.email ?? u.email ?? null,
    displayName,
    raw: rawArr,
  };
}

const Ctx = createContext({
  user: null,
  loading: true,
  login: async () => {},
  logout: async () => {},
  refresh: async () => {},
});

export function UserProvider({ children }) {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    (async () => {
      try {
        const u = await fetchBffUser();
        setUser(u);
      } catch {
        setUser(null);
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  // --- login qua BFF public API ---
  const login = async (username, password) => {
    await ensureCsrfToken();
    await api.post("/bff/public/login", { username, password });
    await ensureCsrfToken(); // CSRF mới theo session
    const u = await fetchBffUser();
    setUser(u);
    return u;
  };

  // --- logout qua BFF public API ---
  const logout = async () => {
    const csrf = readCookie("__Host-bff-csrf");
    await api.post(
      "/bff/public/logout",
      {},
      { headers: csrf ? { "X-CSRF": csrf } : {} },
    );
    setUser(null);
  };

  const refresh = async () => {
    try {
      const u = await fetchBffUser();
      setUser(u);
      return u;
    } catch {
      setUser(null);
      return null;
    }
  };

  const value = useMemo(
    () => ({ user, loading, login, logout, refresh }),
    [user, loading],
  );

  return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
}

UserProvider.propTypes = {
  children: PropTypes.node,
};

export const useUser = () => useContext(Ctx);
