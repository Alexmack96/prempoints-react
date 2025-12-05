import { CreateTeamForm } from '../teams/create-team/CreateTeamForm';
import { useTeamsList } from '../teams/teams-list/useTeamsList';

export const TeamsPage = () => {
  const { data: teams, isLoading, isError, error } = useTeamsList();

  if (isError) {
    return <div className="p-4 text-red-600">Error: {error?.message}</div>;
  }

  return (
    <div style={{ padding: '20px' }}>
      <h1>Team Manager</h1>
      <CreateTeamForm />
      <hr />
      {isLoading ? (
        <p>Loading teams...</p>
      ) : (
        <ul>
          {/* Added defensive check so it doesn't crash if data isn't an array yet */}
          {Array.isArray(teams) && teams.map((team) => <li key={team.id}>{team.teamName}</li>)}
        </ul>
      )}
    </div>
  );
};

export default TeamsPage;
