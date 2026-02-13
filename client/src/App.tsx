import { Toaster } from "react-hot-toast";
import { Route, Routes } from "react-router";
import { UnitListPage } from "./features/UnitListPage";
function App() {
  return (
    <>
      <Toaster />
      <Routes>
        <Route path="/" element={<UnitListPage />} />
      </Routes>
      <div
        dangerouslySetInnerHTML={{
          __html: `
              <h2>AI Output</h2>
              <ul>
                <li>Unit 1: Marine</li>
                <li>Unit 2: Zergling</li>
                <li>Unit 3: Zealot</li>
              </ul>
          `,
        }}
      ></div>
    </>
  );
}

export default App;
