import * as React from 'react';
import { translate, InjectedTranslateProps as i18nProps } from 'react-i18next';

import { UserAttributes } from '@data/models/user';
import { GroupAttributes } from '@data/models/group';
import { isEmpty } from '@lib/collection';

import Header from './header';
import Row from './row';

import './user-table.scss';
import { ResourceObject } from 'jsonapi-typescript';
import { USERS_TYPE, GROUPS_TYPE } from '@data';

interface IOwnProps {
<<<<<<< HEAD
  users: Array<JSONAPI<UserAttributes>>;
  toggleLock: (user: JSONAPI<UserAttributes>) => void;
=======
  users: Array<ResourceObject<USERS_TYPE, UserAttributes>>;
  groups: Array<ResourceObject<GROUPS_TYPE, GroupAttributes>>;
  toggleLock: (user: ResourceObject<USERS_TYPE, UserAttributes>) => void;
>>>>>>> master
}

type IProps =
  & IOwnProps
  & i18nProps;

class Table extends React.Component<IProps> {
  render() {
    const { users, t, toggleLock } = this.props;

    return (
      <table data-test-users className= 'ui table user-table' >
        <Header />
        <tbody>

          { users && users.map((user,index) => (
            <Row key={index} user={user} toggleLock={toggleLock} />
          ))}

          { isEmpty(users) && (
            <tr><td colSpan={5}>
              {t('users.noneFound')}
            </td></tr>
          ) }

        </tbody>
      </table>
    );
  }
}

export default translate('translations')(Table);
