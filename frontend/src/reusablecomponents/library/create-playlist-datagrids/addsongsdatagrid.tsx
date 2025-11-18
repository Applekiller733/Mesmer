import { DataGrid, type GridColDef, type GridRowSelectionModel } from '@mui/x-data-grid';
import FormControlLabel from '@mui/material/FormControlLabel';
import Switch from '@mui/material/Switch';
import Box from '@mui/material/Box';
import { useState } from 'react';

export default function AddSongsGrid(handleRowSelect: any) {
    const [rows, setRows] = useState([]);

    const columns: GridColDef[] = [
        { field: "id", headerName: "ID", width: 90 },
        { field: "name", headerName: "Name", flex: 1 },
        { field: "artist", headerName: "Artist", flex: 1 },
        { field: "upvotes", headerName: "Upvotes", type: "number", width: 120 },
    ];

    // function handleRowChange(){

    // }

    return (
        <div style={{ width: '100%' }}>
            <div style={{ height: 400 }}>
                <DataGrid
                    columns={columns}
                    rows={rows}
                    checkboxSelection={true}
                    onRowSelectionModelChange={handleRowSelect}
                />
            </div>
        </div>
    );
}
