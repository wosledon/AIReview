import React from 'react';
import { useParams } from 'react-router-dom';
import RepositoryStatus from '../components/git/RepositoryStatus';

const RepositoryStatusPage: React.FC = () => {
  const params = useParams();
  const repoId = Number(params.id);
  if (Number.isNaN(repoId)) return <div>无效的仓库 ID</div>;

  return (
    <div className="p-6">
      <RepositoryStatus repositoryId={repoId} />
    </div>
  );
};

export default RepositoryStatusPage;