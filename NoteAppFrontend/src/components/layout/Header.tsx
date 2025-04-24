import { Link, useLocation, useNavigate } from 'react-router-dom';
import { UserCircleIcon, ArrowRightOnRectangleIcon } from '@heroicons/react/24/outline';
import { useAppSelector, useAppDispatch } from '../../store/hooks';
import { useState, useRef, useEffect } from 'react';
import { logout } from '../../store/authSlice';

interface User {
  email: string;
  id: string;
  userName: string;
}

const Header = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const isNotesPage = location.pathname === '/notes';
  const user = useAppSelector(state => state.auth.user) as User | null;
  const [showDropdown, setShowDropdown] = useState(false);
  const timeoutRef = useRef<NodeJS.Timeout>();

  // Сбрасываем состояние дропдауна при изменении страницы
  useEffect(() => {
    setShowDropdown(false);
  }, [location.pathname]);

  const handleMouseEnter = () => {
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
    }
    setShowDropdown(true);
  };

  const handleMouseLeave = () => {
    timeoutRef.current = setTimeout(() => {
      setShowDropdown(false);
    }, 300);
  };

  const handleLogout = async () => {
    setShowDropdown(false); // Скрываем дропдаун перед выходом
    await dispatch(logout());
    navigate('/login');
  };

  return (
    <header className="bg-white shadow-lg">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-20">
          <div className="flex items-center">
            <Link to="/" className="text-2xl font-bold text-indigo-600 hover:text-indigo-700 transition-colors">
              Note Manager
            </Link>
          </div>
          {isNotesPage ? (
            <div className="flex items-center gap-3 relative">
              <span className="text-gray-700 font-medium">{user?.userName || 'User'}</span>
              <div 
                className="relative cursor-pointer"
                onMouseEnter={handleMouseEnter}
                onMouseLeave={handleMouseLeave}
              >
                <UserCircleIcon className="h-8 w-8 text-gray-600 hover:text-indigo-600 transition-colors" />
                {showDropdown && (
                  <div 
                    className="absolute right-0 mt-2 w-48 rounded-md shadow-lg bg-white ring-1 ring-black ring-opacity-5 z-50"
                    onMouseEnter={handleMouseEnter}
                    onMouseLeave={handleMouseLeave}
                  >
                    <div className="py-1">
                      <button
                        onClick={handleLogout}
                        className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 hover:text-gray-900 flex items-center gap-2"
                      >
                        <ArrowRightOnRectangleIcon className="h-5 w-5" />
                        <span>Sign Out</span>
                      </button>
                    </div>
                  </div>
                )}
              </div>
            </div>
          ) : (
            <nav className="flex items-center space-x-8">
              <Link
                to="/login"
                className="text-gray-600 hover:text-gray-900 px-4 py-2 text-base font-medium transition-colors"
              >
                Sign In
              </Link>
              <Link
                to="/register"
                className="text-indigo-600 hover:text-indigo-700 px-4 py-2 text-base font-medium transition-colors"
              >
                Sign Up
              </Link>
            </nav>
          )}
        </div>
      </div>
    </header>
  );
};

export default Header; 