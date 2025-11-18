import { useState } from "react";
import { useAppDispatch } from "../../hooks/hooks";
import type { FileModel } from "../../models/file";
import { uploadFile } from "../../stores/thunks/filethunks";

export default function FileUploader() {
    const [file, setFile] = useState<File | null>(null);
    const [fileName, setFileName] = useState('');
    const [extension, setExtension] = useState('');
    const dispatch = useAppDispatch();

    const saveFile = (e: any) => {
        setFile(e.target.files[0]);
        setFileName(e.target.files[0].name);
        setExtension(e.target.files[0].name.split('.')[1]);
    };

    function handleUpload() {
        let fileRequest: FileModel = {
            file: file,
            filename: fileName,
            extension: extension,
        }
        dispatch(uploadFile(fileRequest));
    }

    return (
        <>
            <input type="file" onChange={saveFile} />
            <input type="button" value="upload" onClick={handleUpload} />
        </>
    );
}