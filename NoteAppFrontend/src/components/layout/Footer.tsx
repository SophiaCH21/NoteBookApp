const Footer = () => {
  return (
    <footer className="bg-white shadow-lg mt-auto">
      <div className="max-w-7xl mx-auto py-6 px-4 sm:px-6 lg:px-8">
        <div className="flex">
          <div className="text-gray-500 text-sm">
            © {new Date().getFullYear()} Note Manager. Dmitry Filin.
          </div>
        </div>
      </div>
    </footer>
  );
};

export default Footer; 