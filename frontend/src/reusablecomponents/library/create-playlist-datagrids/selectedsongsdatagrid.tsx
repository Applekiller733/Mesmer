import { DataGrid, GridActionsCell, GridActionsCellItem, GridDeleteIcon, type GridColDef } from '@mui/x-data-grid';
import FormControlLabel from '@mui/material/FormControlLabel';
import Switch from '@mui/material/Switch';
import Box from '@mui/material/Box';
import { useState } from 'react';
import DeleteIcon from '@mui/icons-material/DeleteOutlined';

export default function SelectedSongsGrid({ handleRowSelect, handleDeleteRow, rows, isEditing }:
    { handleRowSelect: any, handleDeleteRow: any, rows: any, isEditing: boolean }) {

    const columns: GridColDef[] = [
        { field: "id", headerName: "ID", width: 90 },
        { field: "name", headerName: "Name", flex: 1 },
        { field: "artist", headerName: "Artist", flex: 1 },
        { field: "upvotes", headerName: "Upvotes", type: "number", width: 120 },
        {
            field: "actions", headerName: "Actions", type: "actions", width: 90,
            getActions: ({ id }) => {
                if (isEditing) {
                    return [
                        <GridActionsCellItem
                            icon={<DeleteIcon />}
                            label="Delete"
                            onClick={() => { handleDeleteRow(id) }}
                        ></GridActionsCellItem>
                    ];
                }
                return [];
            }
        }
    ];

    return (
        <div style={{ width: '100%' }}>
            <div style={{ height: 400 }}>
                <DataGrid
                    columns={columns}
                    rows={rows}
                    // checkboxSelection={true}
                    onRowSelectionModelChange={handleRowSelect}
                />
            </div>
        </div>
    );
}
