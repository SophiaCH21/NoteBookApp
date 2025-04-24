import axios from 'axios';
import { Note, CreateNoteDto, UpdateNoteDto } from '../store/types';

const API_URL = 'http://localhost:5000/api';

export const notesApi = {
  async fetchNotes(): Promise<Note[]> {
    const response = await axios.get(`${API_URL}/notes`);
    return response.data;
  },

  async createNote(dto: CreateNoteDto): Promise<Note> {
    const response = await axios.post(`${API_URL}/notes`, dto);
    return response.data;
  },

  async updateNote(dto: UpdateNoteDto): Promise<Note> {
    const response = await axios.put(`${API_URL}/notes/${dto.id}`, dto);
    return response.data;
  },

  async deleteNote(id: number): Promise<void> {
    await axios.delete(`${API_URL}/notes/${id}`);
  }
}; 