import React, { useState } from "react";
import axios from "axios";
import type { FileModel } from "../../models/file";

const API_URL = `${import.meta.env.VITE_API_URL}/files`


export function apiuploadfile(request: FormData){
    const url = `${API_URL}`;
    return axios.post(url, request);
}

export function apigetfile(){
    return "NOT IMPLEMENTED";
}
