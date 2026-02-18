import { Toaster } from "react-hot-toast";
import { Route, Routes } from "react-router";
import { UnitListPage } from "./features/UnitListPage";
function App() {``
  return (
    <>
      <Toaster />
      <Routes>
        <Route path="/" element={<UnitListPage />} />
      </Routes>
      
    </>
  );
}

export default App;
