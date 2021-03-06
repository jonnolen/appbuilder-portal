import { compose, withProps } from 'recompose';
import { withData as withOrbit } from 'react-orbitjs';

import {
  OrganizationResource, UserResource, RoleResource,
  UserRoleResource,
  attributesFor,
  idFor,
  relationshipFor,
  recordsWithIdIn
} from '@data';
import { isEmpty, unique } from '@lib/collection';
import { withTranslations, i18nProps } from '@lib/i18n';

import Display from './display';
import { withCurrentUser } from '@data/containers/with-current-user';

interface INeededProps {
  user: UserResource;
  organizations: OrganizationResource[];
  roles: RoleResource[];
}

interface IOwnProps {
  roleNames: string;
}

interface IAfterUserRoles {
  userRoles: UserRoleResource[];
}

type IProps =
& INeededProps
& IOwnProps
& IAfterUserRoles
& i18nProps;

export default compose<IProps, INeededProps>(
  withTranslations,
  withCurrentUser(),
  // share one set of userRoles for the entire list.
  // otherwise the RoleSelect's own withUserRoles will
  // make a call to get the userRoles as a convient default
  withOrbit((props: INeededProps) => {
    const { user } = props;

    return {
      userRoles: q => q.findRelatedRecords(user, 'userRoles'),
    };
  }),
  withProps((props: INeededProps & IAfterUserRoles & i18nProps) => {
    const { userRoles, organizations, roles, t } = props;

    const applicable = userRoles.filter(userRole => {
      const id = idFor(relationshipFor(userRole, 'organization'));

      return recordsWithIdIn(organizations, [id]).length > 0;
    });

    const names = applicable.map(userRole => {
      const roleId = idFor(relationshipFor(userRole, 'role'));
      const role = roles.find(r => r.id === roleId);

      return attributesFor(role).roleName;
    });

    if (isEmpty(names)) {
      return { roleNames: t('users.noRoles') };
    }

    const result = unique(names)
      .sort()
      .join(', ');

    return {
      roleNames: result
    };
  }),
  withProps(({currentUser, user}) => {
    return {
      editable: currentUser.id !== user.id
    };
  })
)(Display);
