import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import App from "./App.tsx";
import {
  MutationCache,
  QueryCache,
  QueryClient,
  QueryClientProvider,
} from "@tanstack/react-query";
import toast from "react-hot-toast";
import { BrowserRouter } from "react-router";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      throwOnError: true,
    },
  },
  queryCache: new QueryCache({
    onError: (error) => {
      toast.error(
        error instanceof Error ? error.message : "An unknown error occurred",
      );
    },
  }),
  mutationCache: new MutationCache({
    onError: (error) => {
      toast.error(
        error instanceof Error ? error.message : "An unknown error occurred",
      );
    },
  }),
});

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <BrowserRouter>
      <QueryClientProvider client={queryClient}>
        <App />
      </QueryClientProvider>
    </BrowserRouter>
  </StrictMode>,
);
