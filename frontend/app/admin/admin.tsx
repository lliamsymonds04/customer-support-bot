import { Header } from '@components/header';
import { useUser } from '~/hooks/auth/use-user';

export function Admin() {
  const { username, role } = useUser();

  return (
    <>
      <Header username={username} role={role} />
      <main className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
        <h1>Admin Panel</h1>
        <p>Manage users and settings here.</p>
      </main>
    </>
  );
}
