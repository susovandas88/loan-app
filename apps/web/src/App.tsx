import { Link, Navigate, Route, Routes } from "react-router-dom";
import ApplicationDetail from "./pages/ApplicationDetail";
import Home from "./pages/Home";
import NewApplication from "./pages/NewApplication";

export default function App() {
  return (
    <>
      <header>
        <Link to="/">Loan Portal</Link>
        <Link to="/apply">Apply</Link>
      </header>
      <main>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/apply" element={<NewApplication />} />
          <Route path="/applications/:id" element={<ApplicationDetail />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </>
  );
}
