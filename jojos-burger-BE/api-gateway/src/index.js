import express from 'express';
import cors from 'cors';
import morgan from 'morgan';
import { createProxyMiddleware } from 'http-proxy-middleware';

const app = express();
app.use(cors({ origin: 'http://localhost:3000', credentials: true }));
app.use(express.json());
app.use(morgan('dev'));

// URL services (local dev)
const USERSVC_URL = 'http://localhost:4001';

// Public routes
app.use('/users', createProxyMiddleware({ target: USERSVC_URL, changeOrigin: true }));
app.use('/sessions', createProxyMiddleware({ target: USERSVC_URL, changeOrigin: true }));

app.listen(4000, () => console.log('API Gateway: http://localhost:4000'));