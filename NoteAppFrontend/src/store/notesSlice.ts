import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import axios, { AxiosError } from 'axios';
import { Note, NotesState, CreateNoteDto, UpdateNoteDto, NoteResponse } from './types';
import { API_BASE_URL } from '../config';
import { getAuthHeader } from '../utils/auth';

interface ErrorResponse {
  message: string;
}

const initialState: NotesState = {
  notes: [],
  selectedNote: null,
  loading: false,
  error: null,
  searchTerm: '',
  dateRange: {
    startDate: null,
    endDate: null
  }
};

// Асинхронные actions
export const fetchNotes = createAsyncThunk<Note[], void>(
  'notes/fetchNotes',
  async (_, { rejectWithValue }) => {
    try {
      const response = await axios.get<NoteResponse[]>(`${API_BASE_URL}/notes`, {
        headers: getAuthHeader()
      });
      // Преобразуем данные с бэкенда в формат фронтенда
      const notes: Note[] = response.data.map(note => ({
        id: note.id,
        title: note.title,
        content: note.description,
        createdAt: note.createdAt,
        updatedAt: note.updatedAt
      }));
      return notes;
    } catch (error) {
      const axiosError = error as AxiosError<ErrorResponse>;
      if (axiosError.response?.status === 401) {
        window.location.href = '/login';
      }
      return rejectWithValue(axiosError.response?.data?.message || 'Не удалось загрузить заметки');
    }
  }
);

export const createNote = createAsyncThunk<Note, CreateNoteDto>(
  'notes/createNote',
  async (dto, { rejectWithValue }) => {
    try {
      const response = await axios.post<NoteResponse>(`${API_BASE_URL}/notes`, {
        title: dto.title,
        description: dto.content
      }, {
        headers: getAuthHeader()
      });
      // Преобразуем ответ с бэкенда в формат фронтенда
      const note: Note = {
        id: response.data.id,
        title: response.data.title,
        content: response.data.description,
        createdAt: response.data.createdAt,
        updatedAt: response.data.updatedAt
      };
      return note;
    } catch (error) {
      const axiosError = error as AxiosError<ErrorResponse>;
      if (axiosError.response?.status === 401) {
        window.location.href = '/login';
      }
      return rejectWithValue(axiosError.response?.data?.message || 'Не удалось создать заметку');
    }
  }
);

export const updateNote = createAsyncThunk<Note, UpdateNoteDto>(
  'notes/updateNote',
  async (dto, { rejectWithValue }) => {
    try {
      const response = await axios.put<NoteResponse>(`${API_BASE_URL}/notes/${dto.id}`, {
        title: dto.title,
        description: dto.content
      }, {
        headers: getAuthHeader()
      });
      // Преобразуем ответ с бэкенда в формат фронтенда
      const note: Note = {
        id: response.data.id,
        title: response.data.title,
        content: response.data.description,
        createdAt: response.data.createdAt,
        updatedAt: response.data.updatedAt
      };
      return note;
    } catch (error) {
      const axiosError = error as AxiosError<ErrorResponse>;
      if (axiosError.response?.status === 401) {
        window.location.href = '/login';
      }
      return rejectWithValue(axiosError.response?.data?.message || 'Не удалось обновить заметку');
    }
  }
);

export const deleteNote = createAsyncThunk<string, string>(
  'notes/deleteNote',
  async (id, { rejectWithValue }) => {
    try {
      await axios.delete(`${API_BASE_URL}/notes/${id}`, {
        headers: getAuthHeader()
      });
      return id;
    } catch (error) {
      const axiosError = error as AxiosError<ErrorResponse>;
      if (axiosError.response?.status === 401) {
        window.location.href = '/login';
      }
      return rejectWithValue(axiosError.response?.data?.message || 'Не удалось удалить заметку');
    }
  }
);

const notesSlice = createSlice({
  name: 'notes',
  initialState,
  reducers: {
    setSelectedNote: (state, action: PayloadAction<Note | null>) => {
      state.selectedNote = action.payload;
    },
    clearError: (state) => {
      state.error = null;
    },
    addNote: (state, action: PayloadAction<Note>) => {
      state.notes.push(action.payload);
    },
    setSearchTerm: (state, action: PayloadAction<string>) => {
      state.searchTerm = action.payload;
    },
    setDateRange: (state, action: PayloadAction<{ startDate: string | null; endDate: string | null }>) => {
      state.dateRange = action.payload;
    }
  },
  extraReducers: (builder) => {
    builder
      // Обработка fetchNotes
      .addCase(fetchNotes.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchNotes.fulfilled, (state, action) => {
        state.loading = false;
        state.notes = action.payload;
      })
      .addCase(fetchNotes.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      })
      // Обработка createNote
      .addCase(createNote.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(createNote.fulfilled, (state, action) => {
        state.loading = false;
        state.notes.push(action.payload);
      })
      .addCase(createNote.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      })
      // Обработка updateNote
      .addCase(updateNote.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(updateNote.fulfilled, (state, action) => {
        state.loading = false;
        const index = state.notes.findIndex(note => note.id === action.payload.id);
        if (index !== -1) {
          state.notes[index] = action.payload;
        }
      })
      .addCase(updateNote.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      })
      // Обработка deleteNote
      .addCase(deleteNote.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(deleteNote.fulfilled, (state, action) => {
        state.loading = false;
        state.notes = state.notes.filter(note => note.id !== action.payload);
      })
      .addCase(deleteNote.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      });
  },
});

export const { setSelectedNote, clearError, addNote, setSearchTerm, setDateRange } = notesSlice.actions;
export default notesSlice.reducer; 