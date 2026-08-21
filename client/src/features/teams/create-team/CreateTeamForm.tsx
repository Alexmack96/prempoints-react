import { useState } from 'react';
import { Plus } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
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
    <form onSubmit={handleSubmit} className="space-y-3">
      <div className="space-y-2">
        <Label htmlFor="team-name">Club name</Label>
        <Input
          id="team-name"
          type="text"
          placeholder="e.g. Nottingham Forest"
          value={name}
          onChange={(e) => setName(e.target.value)}
          disabled={createTeamMutation.isPending}
        />
      </div>

      <Button type="submit" className="w-full" disabled={createTeamMutation.isPending || !name}>
        <Plus className="size-4" />
        {createTeamMutation.isPending ? 'Creating…' : 'Create club'}
      </Button>

      {createTeamMutation.isError && (
        <p className="text-destructive text-sm">
          Error creating team: {createTeamMutation.error.message}
        </p>
      )}
    </form>
  );
};
