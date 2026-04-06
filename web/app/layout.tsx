import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
  title: 'AppPortable Web Demo',
  description: 'Demo web para carga, procesamiento y búsqueda de PDFs.'
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="es">
      <body>{children}</body>
    </html>
  );
}
