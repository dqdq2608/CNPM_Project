import React, {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import PropTypes from "prop-types";
import { bffAuthApi } from "../services/bffAuthApi";

// Chuyển dto từ /bff/public/user thành object gọn cho FE
async function fetchBffUser() {
  // đảm bảo đã handshake CSRF
  await bffAuthApi.initAntiforgery();

  const u = await bffAuthApi.getUser();
  if (!u) throw new Error("No user");

  const rawArr = Array.isArray(u.raw) ? u.raw : [];
  const claim = Object.fromEntries(rawArr.map((c) => [c.type, c.value]));

  // full name
  const full = [claim.given_name, claim.family_name]
    .filter(Boolean)
    .join(" ")
    .trim();

  // tên hiển thị trên header
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

  // load user hiện tại khi app khởi động
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

  // --- login qua BFF Authentication API ---
  const login = async (username, password) => {
    // gọi BFF login (tự lo CSRF + cookie)
    await bffAuthApi.login(username, password);
    const u = await fetchBffUser();
    setUser(u);
    return u;
  };

  // --- logout qua BFF Authentication API ---
  const logout = async () => {
    await bffAuthApi.logout();
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
