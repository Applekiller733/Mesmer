import { createAsyncThunk } from "@reduxjs/toolkit";
import type { FileModel } from "../../models/file";
import { apiuploadfile } from "../api/fileapi";

export const uploadFile = createAsyncThunk(`files/upload-file`, async(file: FileModel) => {
    try{
        if (!file || !file.file) return;
        const formData = new FormData();
        formData.append('formFile', file.file);
        formData.append('fileName', file.filename);
        formData.append('extension', file.extension);

        const response = apiuploadfile(formData);

        if (!response) {
            throw new Error("File Upload Failed");
        }
        return response;
    }
    catch( err: any){
        return err.message;
    }
});
