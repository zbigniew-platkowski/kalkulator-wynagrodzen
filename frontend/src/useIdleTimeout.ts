import { useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';

/**
 * Hook który automatycznie wylogowuje użytkownika po określonym czasie bezczynności.
 * Implementuje WNF-1.1 ze specyfikacji (15 minut bezczynności).
 * 
 * Śledzi aktywność myszy, klawiatury i dotyku.
 * Przy każdej aktywności resetuje licznik.
 */
const useIdleTimeout = (minuty: number = 15) => {
  const navigate = useNavigate();
  const timerRef = useRef<NodeJS.Timeout | null>(null);

  const resetTimer = () => {
    if (timerRef.current) clearTimeout(timerRef.current);

    timerRef.current = setTimeout(() => {
      // Sprawdź czy użytkownik jest zalogowany
      const token = localStorage.getItem('token');
      if (token) {
        localStorage.clear();
        alert(`Zostałeś automatycznie wylogowany z powodu ${minuty} minut bezczynności.`);
        navigate('/login');
      }
    }, minuty * 60 * 1000);
  };

  useEffect(() => {
    const zdarzenia = ['mousedown', 'mousemove', 'keypress', 'scroll', 'touchstart', 'click'];

    zdarzenia.forEach(zdarzenie => {
      window.addEventListener(zdarzenie, resetTimer, true);
    });

    resetTimer(); // Uruchom timer przy montowaniu

    return () => {
      zdarzenia.forEach(zdarzenie => {
        window.removeEventListener(zdarzenie, resetTimer, true);
      });
      if (timerRef.current) clearTimeout(timerRef.current);
    };
  }, []);
};

export default useIdleTimeout;