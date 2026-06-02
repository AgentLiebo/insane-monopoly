declare module '*.css';
declare module 'react';
declare module 'react-dom/client';
declare module 'react/jsx-runtime';
declare module 'vite';
declare module '@vitejs/plugin-react';
declare module 'express';
declare module 'cors';
declare module 'socket.io';
declare module 'better-sqlite3';
declare module 'pg';
declare module 'vitest';
declare module 'node:fs';
declare module 'node:path';
declare module 'node:http';
declare const process: { env: Record<string, string | undefined>; platform: string };
declare namespace JSX { interface IntrinsicElements { [elemName: string]: any } }
