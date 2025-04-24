import { createAsyncThunk } from '@reduxjs/toolkit';
import axios from 'axios';
import { Note, CreateNoteDto, UpdateNoteDto } from './types';
import { setNotes, addNote, updateNote as updateNoteAction, deleteNote as deleteNoteAction, setError, setLoading } from './notesSlice';

const API_URL = 'http://localhost:5000/api';

export const fetchNotes = createAsyncThunk('notes/fetchNotes', async (_, { dispatch }) => {
  try {
    dispatch(setLoading(true));
    const response = await axios.get<Note[]>(`${API_URL}/notes`);
    dispatch(setNotes(response.data));
  } catch (error) {
    dispatch(setError(error instanceof Error ? error.message : 'Ошибка при загрузке заметок'));
    throw error;
  } finally {
    dispatch(setLoading(false));
  }
});

export const createNote = createAsyncThunk(
  'notes/createNote',
  async (noteData: CreateNoteDto, { dispatch }) => {
    try {
      dispatch(setLoading(true));
      const response = await axios.post<Note>(`${API_URL}/notes`, noteData);
      dispatch(addNote(response.data));
      return response.data;
    } catch (error) {
      dispatch(setError(error instanceof Error ? error.message : 'Ошибка при создании заметки'));
      throw error;
    } finally {
      dispatch(setLoading(false));
    }
  }
);

export const updateNote = createAsyncThunk(
  'notes/updateNote',
  async (noteData: UpdateNoteDto, { dispatch }) => {
    try {
      dispatch(setLoading(true));
      const response = await axios.put<Note>(`${API_URL}/notes/${noteData.id}`, noteData);
      dispatch(updateNoteAction(response.data));
      return response.data;
    } catch (error) {
      dispatch(setError(error instanceof Error ? error.message : 'Ошибка при обновлении заметки'));
      throw error;
    } finally {
      dispatch(setLoading(false));
    }
  }
);

export const deleteNote = createAsyncThunk(
  'notes/deleteNote',
  async (noteId: number, { dispatch }) => {
    try {
      dispatch(setLoading(true));
      await axios.delete(`${API_URL}/notes/${noteId}`);
      dispatch(deleteNoteAction(noteId));
    } catch (error) {
      dispatch(setError(error instanceof Error ? error.message : 'Ошибка при удалении заметки'));
      throw error;
    } finally {
      dispatch(setLoading(false));
    }
  }
); 