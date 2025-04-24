import { useState, useCallback, useEffect } from 'react';
import { useDispatch } from 'react-redux';
import { PlusIcon, TrashIcon, ArrowLeftIcon } from '@heroicons/react/24/outline';
import debounce from 'lodash/debounce';
import { useAppSelector } from '../store/hooks';
import { createNote, updateNote, deleteNote, setSelectedNote, fetchNotes } from '../store/notesSlice';
import { Note, CreateNoteDto, UpdateNoteDto } from '../store/types';
import { AppDispatch } from '../store/store';

const NotesPage = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { notes, selectedNote, loading } = useAppSelector(state => state.notes);
  const [showNotesList, setShowNotesList] = useState(true);
  const [localNote, setLocalNote] = useState<Note | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');

  useEffect(() => {
    dispatch(fetchNotes());
  }, [dispatch]);

  useEffect(() => {
    setLocalNote(selectedNote);
  }, [selectedNote]);

  // Создаем мемоизированную функцию для автоматического сохранения
  const debouncedUpdateNote = useCallback(
    debounce((note: UpdateNoteDto) => {
      dispatch(updateNote(note));
    }, 500),
    [dispatch]
  );

  const handleCreateNote = async () => {
    const newNote: CreateNoteDto = {
      title: 'New Note',
      content: ''
    };
    const result = await dispatch(createNote(newNote));
    if (createNote.fulfilled.match(result)) {
      dispatch(setSelectedNote(result.payload));
      if (window.innerWidth < 768) {
        setShowNotesList(false);
      }
    }
  };

  const handleDeleteNote = (noteId: string) => {
    dispatch(deleteNote(noteId));
    // Если удаляется текущая открытая заметка
    if (localNote?.id === noteId) {
      setLocalNote(null);
      setShowNotesList(true);
    }
  };

  const handleNoteSelect = (note: Note) => {
    dispatch(setSelectedNote(note));
    if (window.innerWidth < 768) {
      setShowNotesList(false);
    }
  };

  const handleTitleChange = (newTitle: string) => {
    if (!localNote) return;
    
    const updatedNote = {
      ...localNote,
      title: newTitle
    };
    setLocalNote(updatedNote);
    
    const updateDto: UpdateNoteDto = {
      id: updatedNote.id,
      title: updatedNote.title,
      content: updatedNote.content
    };
    debouncedUpdateNote(updateDto);
  };

  const handleContentChange = (newContent: string) => {
    if (!localNote) return;
    
    const updatedNote = {
      ...localNote,
      content: newContent
    };
    setLocalNote(updatedNote);
    
    const updateDto: UpdateNoteDto = {
      id: updatedNote.id,
      title: updatedNote.title,
      content: updatedNote.content
    };
    debouncedUpdateNote(updateDto);
  };

  // Простая функция фильтрации
  const getFilteredNotes = () => {
    if (!searchTerm && !startDate && !endDate) {
      return notes;
    }

    const searchLower = searchTerm.toLowerCase();
    const start = startDate ? new Date(startDate) : null;
    const end = endDate ? new Date(endDate) : null;

    return notes.filter(note => {
      // Проверяем поиск
      if (searchTerm && 
          !note.title.toLowerCase().includes(searchLower) && 
          !note.content.toLowerCase().includes(searchLower)) {
        return false;
      }

      // Проверяем даты
      const noteDate = new Date(note.createdAt);
      if (start && noteDate < start) return false;
      if (end && noteDate > end) return false;

      return true;
    });
  };

  // Получаем отфильтрованные и отсортированные заметки
  const sortedNotes = [...getFilteredNotes()].sort((a, b) => {
    const dateA = a.updatedAt ? new Date(a.updatedAt) : new Date(a.createdAt);
    const dateB = b.updatedAt ? new Date(b.updatedAt) : new Date(b.createdAt);
    return dateB.getTime() - dateA.getTime();
  });

  return (
    <div className="flex-1 flex flex-col bg-gray-50">
      <div className="max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Поиск и фильтры */}
        <div className="bg-white rounded-2xl shadow-lg p-4 sm:p-6 mb-8">
          <div className="flex flex-col gap-4">
            <div className="flex flex-col md:flex-row gap-4 w-full">
              <div className="w-full md:flex-1">
                <label htmlFor="search" className="block text-sm font-medium text-gray-700 mb-2">
                  Search by title and content
                </label>
                <input
                  id="search"
                  type="text"
                  className="w-full px-4 py-3 border border-gray-300 rounded-xl shadow-sm placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-colors bg-white text-gray-900 appearance-none"
                  placeholder="Enter text to search..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                />
              </div>
              <div className="flex flex-row gap-2 md:w-auto">
                <div className="flex-1 min-w-[140px]">
                  <label htmlFor="startDate" className="block text-sm font-medium text-gray-700 mb-2">
                    Start Date
                  </label>
                  <input
                    id="startDate"
                    type="date"
                    className="w-full px-4 py-3 border border-gray-300 rounded-xl shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-colors"
                    value={startDate}
                    onChange={(e) => setStartDate(e.target.value)}
                  />
                </div>
                <div className="flex-1 min-w-[140px]">
                  <label htmlFor="endDate" className="block text-sm font-medium text-gray-700 mb-2">
                    End Date
                  </label>
                  <input
                    id="endDate"
                    type="date"
                    className="w-full px-4 py-3 border border-gray-300 rounded-xl shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-colors"
                    value={endDate}
                    onChange={(e) => setEndDate(e.target.value)}
                  />
                </div>
                <div className="flex items-end">
                  <button
                    onClick={handleCreateNote}
                    className="h-[51px] inline-flex items-center justify-center px-4 border border-transparent rounded-xl shadow-sm text-base font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 transition-colors"
                  >
                    <PlusIcon className="h-5 w-5" />
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className="flex gap-8">
          {/* Левая панель со списком заметок */}
          <div className={`md:w-80 flex-1 md:flex-none bg-white rounded-2xl shadow-lg overflow-hidden md:block ${showNotesList ? 'block' : 'hidden'}`}>
            <div className="h-[calc(100vh-400px)] overflow-y-auto">
              {loading ? (
                <div className="p-4 text-center text-gray-500">
                  Loading...
                </div>
              ) : sortedNotes.length > 0 ? (
                sortedNotes.map(note => (
                  <div
                    key={note.id}
                    className={`border-b border-gray-100 cursor-pointer transition-all ${
                      selectedNote?.id === note.id
                        ? 'bg-indigo-50'
                        : 'hover:bg-gray-50'
                    }`}
                    onClick={() => handleNoteSelect(note)}
                  >
                    <div className="p-4">
                      <div className="flex items-center justify-between mb-2">
                        <h3 className="font-medium text-gray-900">{note.title}</h3>
                        <div className="flex items-center gap-2">
                          <span className="text-xs text-gray-500">
                            {new Date(note.createdAt).toLocaleDateString('ru-RU')}
                          </span>
                          <button
                            onClick={(e) => {
                              e.stopPropagation();
                              handleDeleteNote(note.id);
                            }}
                            className="p-1 text-gray-400 hover:text-red-600 rounded-lg hover:bg-gray-50 transition-colors"
                          >
                            <TrashIcon className="h-4 w-4" />
                          </button>
                        </div>
                      </div>
                      <p className="text-sm text-gray-500 line-clamp-2">{note.content}</p>
                    </div>
                  </div>
                ))
              ) : (
                <div className="p-4 text-center text-gray-500">
                  {searchTerm || startDate || endDate
                    ? 'No notes matching search criteria'
                    : 'No notes. Create a new note by clicking the \'+\' button'}
                </div>
              )}
            </div>
          </div>

          {/* Правая панель с контентом заметки */}
          <div className={`flex-1 h-[calc(100vh-400px)] ${!showNotesList ? 'block' : 'hidden md:block'}`}>
            {localNote ? (
              <div className="bg-white h-full rounded-2xl shadow-lg p-6">
                <div className="flex justify-between items-start mb-6">
                  <div className="flex items-center gap-4 w-full">
                    <button 
                      className="md:hidden p-2 text-gray-400 hover:text-indigo-600 rounded-lg hover:bg-gray-50 transition-colors"
                      onClick={() => setShowNotesList(true)}
                    >
                      <ArrowLeftIcon className="h-5 w-5" />
                    </button>
                    <input
                      type="text"
                      className="text-2xl font-semibold text-gray-900 bg-white border-0 focus:outline-none focus:ring-0 w-full appearance-none"
                      value={localNote.title}
                      onChange={(e) => handleTitleChange(e.target.value)}
                      placeholder="Note title"
                    />
                  </div>
                  <button
                    onClick={() => handleDeleteNote(localNote.id)}
                    className="p-2 text-gray-400 hover:text-red-600 rounded-lg hover:bg-gray-50 transition-colors"
                  >
                    <TrashIcon className="h-5 w-5" />
                  </button>
                </div>
                <textarea
                  className="w-full h-[calc(100%-80px)] text-gray-900 bg-white border-0 focus:outline-none focus:ring-0 resize-none appearance-none"
                  value={localNote.content}
                  onChange={(e) => handleContentChange(e.target.value)}
                  placeholder="Start typing..."
                />
              </div>
            ) : (
              <div className="bg-white h-full rounded-2xl shadow-lg p-6 text-center text-gray-500">
                Select a note from the list or create a new one
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default NotesPage; 