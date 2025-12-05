import { useMemo, useState } from 'react';
import { AllCommunityModule, ModuleRegistry, themeQuartz } from 'ag-grid-community';
import type { ColDef, GridOptions } from 'ag-grid-community';
import { AgGridReact } from 'ag-grid-react';

// Register modules immediately
ModuleRegistry.registerModules([AllCommunityModule]);

type CreatePriceInput = { teamName: string; price: number; valueDate: string };

const myTheme = themeQuartz.withParams({
  spacing: 12,
  accentColor: 'oklch(38.1% 0.176 304.987)',
});

export const PricesPage = () => {
  const defaultColDef = useMemo(() => {
    return {
      flex: 1,
      filter: true,
      floatingFilter: true,
      editable: true,
    };
  }, []);

  const [rowData, setRowData] = useState<CreatePriceInput[]>([
    { teamName: 'Chelsea', price: 70, valueDate: '2025-11-12' },
    { teamName: 'Liverpool', price: 60, valueDate: '2025-11-12' },
    { teamName: 'Arsenal', price: 50, valueDate: '2025-11-12' },
  ]);

  const [colDefs, setColDefs] = useState<ColDef<CreatePriceInput>[]>([
    {
      field: 'teamName',
      cellEditor: 'agSelectCellEditor',
      cellEditorParams: {
        values: [
          'Arsenal',
          'Aston Villa',
          'Bournemouth',
          'Brentford',
          'Brighton',
          'Burnley',
          'Chelsea',
          'Crystal Palace',
          'Everton',
          'Fulham',
          'Leeds',
          'Liverpool',
          'Manchester City',
          'Manchester United',
          'Newcastle',
          'Nottingham Forest',
          'Sunderland',
          'Tottenham',
          'West Ham',
          'Wolves',
        ],
      },
    },
    { field: 'price' },
    { field: 'valueDate' },
  ]);

  const gridOptions: GridOptions = {
    defaultColDef: defaultColDef,
    rowData: rowData,
    columnDefs: colDefs,
  };

  return (
    <div className="ag-theme-quartz w-full h-[500px] px-4 sm:px-6 mt-6">
      <AgGridReact<CreatePriceInput>
        gridOptions={gridOptions}
        rowData={rowData}
        columnDefs={colDefs}
        defaultColDef={defaultColDef}
        theme={myTheme}
      />
    </div>
  );
};

export default PricesPage;
