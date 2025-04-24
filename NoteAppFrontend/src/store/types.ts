export interface Note {
  id: string;
  title: string;
  content: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateNoteDto {
  title: string;
  content: string;
}

export interface UpdateNoteDto {
  id: string;
  title: string;
  content: string;
}

export interface NotesState {
  notes: Note[];
  selectedNote: Note | null;
  loading: boolean;
  error: string | null;
  searchTerm: string;
  dateRange: {
    startDate: string | null;
    endDate: string | null;
  };
}

export interface AuthState {
  isAuthenticated: boolean;
  user: {
    id: number;
    email: string;
  } | null;
  loading: boolean;
  error: string | null;
}

export interface RootState {
  notes: NotesState;
  auth: AuthState;
}

// Типы для ответов с бэкенда
export interface NoteResponse {
  id: string;
  title: string;
  description: string;
  createdAt: string;
  updatedAt: string | null;
} 