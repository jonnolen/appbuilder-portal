import * as React from 'react';

import { compose } from 'recompose';
import { requireAuth } from '@lib/auth';
import { Container, Header, Grid } from 'semantic-ui-react';

import EditProfileForm from './form';

import './profile.scss';

export const pathName = '/profile';


class Profile extends React.Component {
  render() {
    return (
      <Container className='profile'>
        <h1>Profile</h1>
        <Grid>
          <Grid.Row>
            <Grid.Column width={4}>
              <h2>Profile Picture</h2>
            </Grid.Column>
            <Grid.Column width={12}>
              <h2>General</h2>
              <EditProfileForm />
            </Grid.Column>
          </Grid.Row>
        </Grid>

      </Container>
    );
  }
}

export default compose(
  requireAuth
)(Profile);