import React, {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import api, { ensureCsrfToken } from "../services/api";
import PropTypes from "prop-types";

// đọc /bff/user → map claim -> value (+displayName)
async function fetchBffUser() {
  await ensureCsrfToken();
  const r = await api.get("/bff/user");
  const arr = r.data || [];
  const map = Object.fromEntries(arr.map((c) => [c.type, c.value]));

  // build displayName "đẹp"
  const dn =
    map.name ||
    map.preferred_username ||
    map.email ||
    [map.given_name, map.family_name].filter(Boolean).join(" ") ||
    map.sub ||
    "User";

  map.displayName = dn;
  return map;
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

  const login = async (username, password) => {
    await ensureCsrfToken();
    await api.post("/auth/password-login", { username, password });
    await ensureCsrfToken(); // CSRF mới theo session
    const u = await fetchBffUser();
    setUser(u);
    return u;
  };

  const logout = async () => {
    await api.post("/auth/logout", {}); // interceptor tự gắn X-CSRF
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
