import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import apiClient from '../../lib/apiClient';
import { useAuth, type AuthResult } from './AuthContext';
import { useGoogleAuth } from './useGoogleAuth';

export function RegisterPage() {
  const { login } = useAuth();
  const navigate = useNavigate();

  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const signInWithGoogle = useGoogleAuth(setError);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const { data } = await apiClient.post<AuthResult>('/api/auth/register', {
        email,
        password,
        firstName,
        lastName,
      });
      login(data);
      navigate('/');
    } catch (err: unknown) {
      if (err && typeof err === 'object' && 'response' in err) {
        const axiosErr = err as { response: { data?: { error?: string }; status: number } };
        const msg =
          axiosErr.response.data?.error ??
          `Server error (${axiosErr.response.status})`;
        setError(msg);
      } else if (err && typeof err === 'object' && 'message' in err) {
        setError(`Could not reach the server: ${(err as { message: string }).message}`);
      } else {
        setError('Registration failed. Please try again.');
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="min-h-screen flex items-center justify-center p-0 lg:p-6 bg-surface font-body text-on-surface antialiased">
      <div className="w-full max-w-7xl h-full lg:h-[850px] bg-surface-container-lowest lg:rounded-[2.5rem] shadow-[0px_20px_40px_rgba(26,28,28,0.06)] overflow-hidden flex flex-col lg:flex-row">

        {/* Left side — editorial panel */}
        <div className="hidden lg:block lg:w-1/2 relative bg-primary overflow-hidden">
          <img
            className="absolute inset-0 w-full h-full object-cover mix-blend-multiply opacity-80"
            src="https://lh3.googleusercontent.com/aida-public/AB6AXuDHW7GOVwonPujsWMm7FmXNoSiwwtkoy46eY38WOwQHxjSfDdAaVA9xaI0zfePGthT4wcKbp1nlxAfarqENPmQeT0IGAZtwjoz7nvbXRpKnB1_Bb89qjuZHjxmYgxoGJe4mFfmjBTLvGxGFBjOREX-tcFbDLondeAqznI1v0Hf4JwzCaQDwS5ElxQNRq5TJjNzFHlDhlDb7KNIdg6UqqSJQxQeIach5FbknVHUi5kOGWdv-HbZ3cGHL9pNAxtEDpie9HMqiH-jHjQ"
            alt="Two neighbors smiling while exchanging a vintage film camera"
          />
          <div className="absolute inset-0 bg-gradient-to-t from-primary/80 via-primary/20 to-transparent" />
          <div className="relative h-full flex flex-col justify-end p-16 z-10">
            <div className="mb-8">
              <span className="text-on-primary font-headline text-5xl font-extrabold tracking-tighter leading-tight">
                Start borrowing<br />today.
              </span>
              <p className="text-on-primary-container mt-6 text-xl max-w-md font-medium leading-relaxed opacity-90">
                Create your free account and unlock access to thousands of items available for rent in your community.
              </p>
            </div>
            <div className="flex flex-wrap gap-3 mt-4">
              <div className="px-5 py-2 rounded-full flex items-center gap-2 border border-white/20 bg-white/80 backdrop-blur-sm">
                <span className="material-symbols-outlined text-primary text-sm" style={{ fontVariationSettings: "'FILL' 1" }}>verified</span>
                <span className="text-primary font-bold text-sm tracking-tight">Verified Owners</span>
              </div>
              <div className="px-5 py-2 rounded-full flex items-center gap-2 border border-white/20 bg-white/80 backdrop-blur-sm">
                <span className="material-symbols-outlined text-primary text-sm" style={{ fontVariationSettings: "'FILL' 1" }}>security</span>
                <span className="text-primary font-bold text-sm tracking-tight">Secure Payments</span>
              </div>
            </div>
          </div>
        </div>

        {/* Right side — auth form */}
        <div className="w-full lg:w-1/2 flex flex-col bg-surface-container-lowest">
          {/* Mobile logo */}
          <div className="lg:hidden p-8">
            <span className="text-2xl font-black text-primary tracking-tight font-headline">Borro</span>
          </div>

          <div className="flex-1 flex flex-col justify-center px-8 sm:px-12 lg:px-24 py-12">
            <div className="mb-10">
              <h1 className="font-headline text-3xl font-extrabold text-on-surface tracking-tight mb-3">
                Create an account
              </h1>
              <p className="text-on-surface-variant text-lg">
                Join the community. It's free.
              </p>
            </div>

            {error && (
              <div className="mb-6 px-5 py-4 rounded-2xl bg-error-container border border-error/20 text-on-error-container text-sm font-medium">
                {error}
              </div>
            )}

            {/* Social sign-up */}
            <div className="mb-8">
              <button
                type="button"
                onClick={() => signInWithGoogle()}
                className="w-full flex items-center justify-center gap-3 py-3.5 px-6 rounded-xl bg-surface-container-low border border-outline-variant/15 hover:bg-surface-container-high transition-all duration-200 active:scale-95"
              >
                <svg className="w-5 h-5" viewBox="0 0 24 24">
                  <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4" />
                  <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853" />
                  <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l3.66-2.84z" fill="#FBBC05" />
                  <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335" />
                </svg>
                <span className="font-bold text-on-surface font-label text-sm">Continue with Google</span>
              </button>
            </div>

            <div className="relative mb-8 flex items-center justify-center">
              <div className="absolute inset-0 flex items-center">
                <div className="w-full border-t border-outline-variant/20" />
              </div>
              <span className="relative px-4 bg-surface-container-lowest text-on-surface-variant text-xs font-bold uppercase tracking-widest">
                Or sign up with email
              </span>
            </div>

            <form className="space-y-5" onSubmit={handleSubmit}>
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <label htmlFor="firstName" className="block text-sm font-bold text-on-surface-variant ml-1 font-label">
                    First Name
                  </label>
                  <input
                    id="firstName"
                    type="text"
                    autoComplete="given-name"
                    required
                    value={firstName}
                    onChange={(e) => setFirstName(e.target.value)}
                    placeholder="Jane"
                    className="w-full px-5 py-4 bg-surface-container-low border-0 rounded-2xl focus:ring-2 focus:ring-primary/20 focus:bg-surface-container-lowest transition-all text-on-surface placeholder:text-outline/60 outline-none"
                  />
                </div>
                <div className="space-y-2">
                  <label htmlFor="lastName" className="block text-sm font-bold text-on-surface-variant ml-1 font-label">
                    Last Name
                  </label>
                  <input
                    id="lastName"
                    type="text"
                    autoComplete="family-name"
                    required
                    value={lastName}
                    onChange={(e) => setLastName(e.target.value)}
                    placeholder="Doe"
                    className="w-full px-5 py-4 bg-surface-container-low border-0 rounded-2xl focus:ring-2 focus:ring-primary/20 focus:bg-surface-container-lowest transition-all text-on-surface placeholder:text-outline/60 outline-none"
                  />
                </div>
              </div>

              <div className="space-y-2">
                <label htmlFor="email" className="block text-sm font-bold text-on-surface-variant ml-1 font-label">
                  Email Address
                </label>
                <input
                  id="email"
                  type="email"
                  autoComplete="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="name@example.com"
                  className="w-full px-5 py-4 bg-surface-container-low border-0 rounded-2xl focus:ring-2 focus:ring-primary/20 focus:bg-surface-container-lowest transition-all text-on-surface placeholder:text-outline/60 outline-none"
                />
              </div>

              <div className="space-y-2">
                <label htmlFor="password" className="block text-sm font-bold text-on-surface-variant ml-1 font-label">
                  Password
                </label>
                <div className="relative">
                  <input
                    id="password"
                    type={showPassword ? 'text' : 'password'}
                    autoComplete="new-password"
                    required
                    minLength={8}
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    placeholder="Min. 8 characters"
                    className="w-full px-5 py-4 bg-surface-container-low border-0 rounded-2xl focus:ring-2 focus:ring-primary/20 focus:bg-surface-container-lowest transition-all text-on-surface outline-none"
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword((v) => !v)}
                    className="absolute right-4 top-1/2 -translate-y-1/2 text-on-surface-variant"
                    aria-label={showPassword ? 'Hide password' : 'Show password'}
                  >
                    <span className="material-symbols-outlined text-xl">
                      {showPassword ? 'visibility_off' : 'visibility'}
                    </span>
                  </button>
                </div>
              </div>

              <button
                type="submit"
                disabled={loading}
                className="w-full py-4 bg-primary text-on-primary rounded-full font-headline font-bold text-lg shadow-lg shadow-primary/20 hover:scale-[1.02] active:scale-95 transition-all duration-200 disabled:opacity-60 disabled:cursor-not-allowed disabled:scale-100"
              >
                {loading ? 'Creating account…' : 'Create Account'}
              </button>
            </form>

            <div className="mt-8 text-center">
              <p className="text-on-surface-variant font-medium">
                Already have an account?{' '}
                <Link to="/login" className="text-tertiary font-extrabold hover:underline ml-1">
                  Log In
                </Link>
              </p>
            </div>
          </div>

          {/* Footer */}
          <div className="p-8 flex justify-center gap-6 text-xs font-bold text-on-surface-variant opacity-60 tracking-tight">
            <span className="hover:text-primary transition-colors cursor-default">Privacy Policy</span>
            <span className="hover:text-primary transition-colors cursor-default">Terms of Service</span>
            <span className="hover:text-primary transition-colors cursor-default">Help Center</span>
          </div>
        </div>
      </div>

      {/* Background accents */}
      <div className="fixed top-[-10%] right-[-5%] w-[400px] h-[400px] bg-primary/5 rounded-full blur-3xl -z-10" />
      <div className="fixed bottom-[-10%] left-[-5%] w-[300px] h-[300px] bg-tertiary/5 rounded-full blur-3xl -z-10" />
    </main>
  );
}
