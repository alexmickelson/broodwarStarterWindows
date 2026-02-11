import { useQuery, useSuspenseQuery } from "@tanstack/react-query";
import { z } from "zod";

export const WebUnitInfoSchema = z.object({
  id: z.number(),
  unitTypeName: z.string(),
  currentOrder: z.string(),
});

export type WebUnitInfo = z.infer<typeof WebUnitInfoSchema>;


function fetchUnitInfo(): Promise<WebUnitInfo[]> {
  return fetch("http://localhost:5200/api/unit-data")
    .then((res) => res.json())
    .then((data) => z.array(WebUnitInfoSchema).parse(data));
}

export const starcraftKeys = {
  units: ["unitData"] as const,
};

export const useUnitQuery = () => {
  return useSuspenseQuery({
    queryKey: starcraftKeys.units,
    queryFn: fetchUnitInfo,
  });
};
