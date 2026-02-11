import { Suspense } from "react";
import { ErrorBoundary } from "react-error-boundary";
import { Spinner } from "../components/Spinner";
import { UnitList } from "./UnitList";

export const UnitListPage = () => {
  return (
    <ErrorBoundary
      fallbackRender={({ error }) => <div>{error?.toString()}</div>}
    >
      <Suspense fallback={<Spinner />}>
        <UnitList />
      </Suspense>
    </ErrorBoundary>
  );
};
