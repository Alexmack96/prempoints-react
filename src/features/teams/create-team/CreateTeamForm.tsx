import { useState } from 'react';
import { useCreateTeam } from './useCreateTeam';

export const CreateTeamForm = () => {
  const [name, setName] = useState('');
  const createTeamMutation = useCreateTeam();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    if (!name) return;

    // Send 'teamName' to match the C# Request record
    createTeamMutation.mutate(
      { teamName: name },
      {
        onSuccess: () => {
          setName(''); // Clear form on success
        },
      },
    );
  };

  return (
    <form onSubmit={handleSubmit} style={{ display: 'flex', gap: '8px', marginBottom: '20px' }}>
      <input
        type="text"
        placeholder="Enter team name"
        value={name}
        onChange={(e) => setName(e.target.value)}
        disabled={createTeamMutation.isPending}
        style={{ padding: '8px', borderRadius: '4px', border: '1px solid #ccc' }}
      />

      <button
        type="submit"
        disabled={createTeamMutation.isPending}
        style={{ padding: '8px 16px', cursor: 'pointer' }}
      >
        {createTeamMutation.isPending ? 'Creating...' : 'Create Team'}
      </button>

      {createTeamMutation.isError && (
        <div style={{ color: 'red', marginTop: '8px' }}>
          Error creating team: {createTeamMutation.error.message}
        </div>
      )}
    </form>
  );
};
