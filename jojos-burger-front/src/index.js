import React from "react";
import ReactDOM from "react-dom/client";
import { ToastContainer } from "react-toastify";

import AppProvider from "./hooks";
import Routes from "./routes/routes";
import GlobalStyles from "./styles/globalStyles";

function BffProbe() {
  React.useEffect(() => {
    const base = process.env.REACT_APP_API_BASE || ""; // đặt trong .env
    fetch(`${base}/bff/user`, { credentials: "include" })
      .then((r) => r.text().then((t) => ({ status: r.status, body: t })))
      .then(({ status, body }) => {
        console.log("[BFF TEST] /bff/user status:", status);
        console.log("[BFF TEST] body:", body); // sẽ là 401 khi chưa login
      })
      .catch((err) => console.error("[BFF TEST] fetch error:", err));
  }, []);
  return null;
}

const root = ReactDOM.createRoot(document.getElementById("root"));
root.render(
  <React.StrictMode>
    <>
      <AppProvider>
        <BffProbe />
        <Routes />
      </AppProvider>
      <GlobalStyles />
      <ToastContainer />
    </>
  </React.StrictMode>,
);
