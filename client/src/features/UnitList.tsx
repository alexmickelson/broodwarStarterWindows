import { useUnitQuery } from "../starcraftHooks";

export const UnitList = () => {
  const unitQuery = useUnitQuery();
  console.log(unitQuery.data);
  return (
    <div>
      {unitQuery.data?.map((unit) => (
        <div key={unit.id}>
          <h2>{unit.unitTypeName}</h2>
          <p>Current Order: {unit.currentOrder}</p>
        </div>
      ))}
    </div>
  );
};
