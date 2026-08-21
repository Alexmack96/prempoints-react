import { useQueryClient } from '@tanstack/react-query';
import { teamKeys } from '../teamKeys';
import { useTeamsList } from './useTeamsList';

export const TeamsList = () => {
  const queryClient = useQueryClient();
  const { isLoading } = useTeamsList();

  const handleCancel = () => {
    // This triggers the signal.aborted = true
    queryClient.cancelQueries({ queryKey: teamKeys.lists() });
  };

  if (isLoading) {
    return (
      <div>
        <p>Loading teams...</p>
        <button onClick={handleCancel}>Cancel Request</button>
      </div>
    );
  }

  return <div>{/* List your teams */}</div>;
};
