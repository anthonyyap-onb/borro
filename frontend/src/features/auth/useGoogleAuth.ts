import { useGoogleLogin } from '@react-oauth/google';
import { useNavigate } from 'react-router-dom';
import apiClient from '../../lib/apiClient';
import { useAuth, type AuthResult } from './AuthContext';

export function useGoogleAuth(onError: (msg: string) => void) {
  const { login } = useAuth();
  const navigate = useNavigate();

  const signInWithGoogle = useGoogleLogin({
    onSuccess: async (tokenResponse) => {
      try {
        // Send the access token to backend — backend verifies independently with Google
        const { data } = await apiClient.post<AuthResult>('/api/auth/google', {
          accessToken: tokenResponse.access_token,
        });
        login(data);
        navigate('/');
      } catch (err: unknown) {
        if (err && typeof err === 'object' && 'response' in err) {
          const axiosErr = err as { response: { data?: { error?: string }; status: number } };
          onError(axiosErr.response.data?.error ?? `Server error (${axiosErr.response.status})`);
        } else {
          onError('Google sign-in failed. Please try again.');
        }
      }
    },
    onError: () => {
      onError('Google sign-in was cancelled or failed.');
    },
  });

  return signInWithGoogle;
}

