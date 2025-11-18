import Box from '@mui/material/Box';
import Tooltip from '@mui/material/Tooltip';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/DeleteOutlined';
import SaveIcon from '@mui/icons-material/Save';
import CancelIcon from '@mui/icons-material/Close';
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
    type GridSlotProps,
    Toolbar,
    ToolbarButton,
} from '@mui/x-data-grid';
import { useEffect, useState } from 'react';
import { useAppDispatch } from '../../hooks/hooks';
import { deleteuser, fetchUsers, update } from '../../stores/thunks/userthunks';
import { useSelector } from 'react-redux';
import { selectAllUsers } from '../../stores/slices/userdataslice';
import type { UpdateUserRequest } from '../../models/user';
import { Button, Dialog, DialogActions, DialogTitle } from '@mui/material';

const roles = ['Admin', 'User'];

const initialRows: GridRowsProp = [

];

declare module '@mui/x-data-grid' {
    interface ToolbarPropsOverrides {
        setRows: (newRows: (oldRows: GridRowsProp) => GridRowsProp) => void;
        setRowModesModel: (
            newModel: (oldModel: GridRowModesModel) => GridRowModesModel,
        ) => void;
    }
}

function EditToolbar(props: GridSlotProps['toolbar']) {
    //   const { setRows, setRowModesModel } = props;

    const handleClick = () => {
        // const id = randomId();
        // setRows((oldRows) => [
        //   ...oldRows,
        //   { id, name: '', age: '', role: '', isNew: true },
        // ]);
        // setRowModesModel((oldModel) => ({
        //   ...oldModel,
        //   [id]: { mode: GridRowModes.Edit, fieldToFocus: 'name' },
        // }));
    };

    return (
        <Toolbar>
            <Tooltip title="Add record">
                <ToolbarButton onClick={handleClick}>
                    {/* <AddIcon fontSize="small" /> */}
                </ToolbarButton>
            </Tooltip>
        </Toolbar>
    );
}

//Dialog does not currently work
//todo: fix?
export default function AdminUserGrid() {
    const [rows, setRows] = useState(initialRows);
    const [rowModesModel, setRowModesModel] = useState<GridRowModesModel>({});
    const dispatch = useAppDispatch();
    const users = useSelector(selectAllUsers);

    // const [deletingRowId, setDeletingRowId] = useState<GridRowId>('');
    // const [dialogOpen, setDialogOpen] = useState(false);

    useEffect(() => {
        dispatch(fetchUsers());
    }, [])

    useEffect(() => {
        console.log(users);
        const newrows = users.map(u => {
            console.log(u.username);
            return {
                id: u.id,
                name: u.username,
                role: u.role,
            }
        }
        );
        // console.log(newrows);
        setRows(newrows);
    }, [users])

    // const handleDialogClose = () => {
    //     setDialogOpen(false);
    // }

    // const handleDialogConfirm = (id: GridRowId) => () => {
    //     setDialogOpen(false);
    //     handleDeleteClick(id);
    // }

    // const handleDialogOpen = (id: GridRowId) => () => {
    //     setDialogOpen(true);
    //     setDeletingRowId(id);
    // }

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

    const handleDeleteClick = (id: GridRowId) => () => {
        dispatch(deleteuser(id as number));

        setRows(rows.filter((row) => row.id !== id));
    };

    const handleCancelClick = (id: GridRowId) => () => {
        setRowModesModel({
            ...rowModesModel,
            [id]: { mode: GridRowModes.View, ignoreModifications: true },
        });

        const editedRow = rows.find((row) => row.id === id);
        if (editedRow!.isNew) {
            setRows(rows.filter((row) => row.id !== id));
        }
    };

    const processRowUpdate = async (newRow: GridRowModel) => {
        const updatedRow = { ...newRow, isNew: false };

        const updaterequest: UpdateUserRequest = {
            id: newRow.id,
            username: newRow.username,
            role: newRow.role,
            email: null,
            password: null,
            confirmpassword: null,
            profilepicture: null,
        };
        //todo test
        const response = await dispatch(update(updaterequest));
        if (response.meta.requestStatus === 'fulfilled') {

            setRows(rows.map((row) => (row.id === newRow.id ? updatedRow : row)));
            return updatedRow;
        }
        else {
            console.log("Updating User from grid failed");
        }
    };

    const handleRowModesModelChange = (newRowModesModel: GridRowModesModel) => {
        setRowModesModel(newRowModesModel);
    };

    const columns: GridColDef[] = [
        { field: 'id', headerName: 'Id', width: 80, editable: false },
        {
            field: 'name',
            headerName: 'Username',
            width: 120,
            editable: true,
        },
        {
            field: 'role',
            headerName: 'Role',
            width: 220,
            editable: true,
            type: 'singleSelect',
            valueOptions: ['Admin', 'User'],
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
                            material={{
                                sx: {
                                    color: 'primary.main',
                                },
                            }}
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
                    <GridActionsCellItem
                        icon={<DeleteIcon />}
                        label="Delete"
                        // onClick={handleDialogOpen(id)}
                        onClick={handleDeleteClick(id)}
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
                '& .actions': {
                    color: 'text.secondary',
                },
                '& .textPrimary': {
                    color: 'text.primary',
                },
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
                slots={{ toolbar: EditToolbar }}
                slotProps={{
                    toolbar: { setRows, setRowModesModel },
                }}
                showToolbar
            />

            {/* <Dialog
                open={dialogOpen}
                onClose={handleDialogClose}>
                <DialogTitle>
                    Are you sure you wish to delete this user?
                </DialogTitle>
                <DialogActions>
                    <Button onClick={handleDialogConfirm(deletingRowId)}>Yes</Button>
                    <Button onClick={handleDialogClose}>Cancel</Button>
                </DialogActions>
            </Dialog> */}
        </Box>
    );
}