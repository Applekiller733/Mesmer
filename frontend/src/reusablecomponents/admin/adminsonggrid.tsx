import {
    type GridRowsProp,
    type GridRowModesModel,
    GridRowModes,
    DataGrid,
    type GridColDef,
    GridActionsCellItem,
    type GridEventListener,
    type GridRowId,
    type GridRowModel,
    GridRowEditStopReasons,
} from '@mui/x-data-grid';
import Box from '@mui/material/Box';
import EditIcon from '@mui/icons-material/Edit';
import SaveIcon from '@mui/icons-material/Save';
import CancelIcon from '@mui/icons-material/Close';
import { useEffect, useState } from 'react';

import { useAppDispatch } from '../../hooks/hooks';
import { useSelector } from 'react-redux';
import { selectAllSongs } from '../../stores/slices/songdataslice';
import { fetchSongs, updateSong } from '../../stores/thunks/songthunks';
import { GENRES, type Genre } from '../../models/genre';
import type { UpdateSongRequest } from '../../models/song';

const initialRows: GridRowsProp = [];

export default function AdminSongGrid() {
    const dispatch = useAppDispatch();
    const songs = useSelector(selectAllSongs);

    const [rows, setRows] = useState(initialRows);
    const [rowModesModel, setRowModesModel] = useState<GridRowModesModel>({});

    useEffect(() => {
        dispatch(fetchSongs());
    }, [dispatch]);

    useEffect(() => {
        // Project the Redux song list into the grid's row shape.
        // Genre falls back to 'Unknown' for songs the API returned
        // without an explicit value — keeps the dropdown stable.
        const newrows = songs.map((s) => ({
            id: s.id,
            name: s.name,
            artist: s.artist,
            genre: (s.genre ?? 'Unknown') as Genre,
        }));
        setRows(newrows);
    }, [songs]);

    const handleRowEditStop: GridEventListener<'rowEditStop'> = (params, event) => {
        if (params.reason === GridRowEditStopReasons.rowFocusOut) {
            event.defaultMuiPrevented = true;
        }
    };

    const handleEditClick = (id: GridRowId) => () => {
        setRowModesModel({ ...rowModesModel, [id]: { mode: GridRowModes.Edit } });
    };

    const handleSaveClick = (id: GridRowId) => () => {
        setRowModesModel({ ...rowModesModel, [id]: { mode: GridRowModes.View } });
    };

    const handleCancelClick = (id: GridRowId) => () => {
        setRowModesModel({
            ...rowModesModel,
            [id]: { mode: GridRowModes.View, ignoreModifications: true },
        });
    };

    const handleRowModesModelChange = (newRowModesModel: GridRowModesModel) => {
        setRowModesModel(newRowModesModel);
    };

    const processRowUpdate = async (newRow: GridRowModel) => {
        // Partial update — only fields editable on this grid are sent.
        // The backend service treats null/undefined as "leave alone",
        // so unchanged fields won't be clobbered.
        const updaterequest: UpdateSongRequest = {
            id: newRow.id,
            name: newRow.name,
            artist: newRow.artist,
            genre: newRow.genre,
        };

        const response = await dispatch(updateSong(updaterequest));
        if (response.meta.requestStatus === 'fulfilled') {
            const updatedRow = { ...newRow, isNew: false };
            setRows(rows.map((row) => (row.id === newRow.id ? updatedRow : row)));
            return updatedRow;
        }

        console.log('Updating Song from grid failed');
        // Returning the old row keeps the grid in sync with what the
        // server actually has — the failed edit is discarded.
        return rows.find((r) => r.id === newRow.id) ?? newRow;
    };

    const columns: GridColDef[] = [
        { field: 'id', headerName: 'Id', width: 280, editable: false },
        {
            field: 'name',
            headerName: 'Name',
            width: 220,
            editable: true,
        },
        {
            field: 'artist',
            headerName: 'Artist',
            width: 180,
            editable: true,
        },
        {
            field: 'genre',
            headerName: 'Genre',
            width: 160,
            editable: true,
            type: 'singleSelect',
            valueOptions: GENRES,
        },
        {
            field: 'actions',
            type: 'actions',
            headerName: 'Actions',
            width: 100,
            cellClassName: 'actions',
            getActions: ({ id }) => {
                const isInEditMode = rowModesModel[id]?.mode === GridRowModes.Edit;

                if (isInEditMode) {
                    return [
                        <GridActionsCellItem
                            icon={<SaveIcon />}
                            label="Save"
                            material={{ sx: { color: 'primary.main' } }}
                            onClick={handleSaveClick(id)}
                        />,
                        <GridActionsCellItem
                            icon={<CancelIcon />}
                            label="Cancel"
                            className="textPrimary"
                            onClick={handleCancelClick(id)}
                            color="inherit"
                        />,
                    ];
                }

                return [
                    <GridActionsCellItem
                        icon={<EditIcon />}
                        label="Edit"
                        className="textPrimary"
                        onClick={handleEditClick(id)}
                        color="inherit"
                    />,
                ];
            },
        },
    ];

    return (
        <Box
            sx={{
                height: 500,
                width: '100%',
                '& .actions': { color: 'text.secondary' },
                '& .textPrimary': { color: 'text.primary' },
            }}
        >
            <DataGrid
                rows={rows}
                columns={columns}
                editMode="row"
                rowModesModel={rowModesModel}
                onRowModesModelChange={handleRowModesModelChange}
                onRowEditStop={handleRowEditStop}
                processRowUpdate={processRowUpdate}
            />
        </Box>
    );
}