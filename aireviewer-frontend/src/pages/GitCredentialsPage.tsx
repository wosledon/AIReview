import React from 'react';
import CredentialManagement from '../components/git/CredentialManagement';

const GitCredentialsPage: React.FC = () => {
  return (
    <div className="p-6">
      <CredentialManagement />
    </div>
  );
};

export default GitCredentialsPage;