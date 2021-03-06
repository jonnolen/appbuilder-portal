import * as React from 'react';
import { compose, withProps } from 'recompose';
import { withData as withCache } from 'react-orbitjs';

import { withSorting } from '@data/containers/api/sorting';
import { withPagination } from '@data/containers/api/pagination';
import { withFiltering } from '@data/containers/api/with-filtering';
import { withLoader } from '@data/containers/with-loader';
import { withNetwork } from '@data/containers/resources/project/list';
import { withCurrentOrganization } from '@data/containers/with-current-organization';
import { TYPE_NAME as PROJECT } from '@data/models/project';

import { withTableColumns, COLUMN_KEY } from '@ui/components/project-table';

import Display from './display';

export const pathName = '/projects/archived';

export default compose(
  withCurrentOrganization,
  withSorting({ defaultSort: 'name' }),
  withPagination(),
  withFiltering({
    requiredFilters: [
      { attribute: 'date-archived', value: 'isnotnull:' }
    ]
  }),
  withNetwork(),
  withLoader(({ error, projects }) => !error && !projects),
  withProps(({ projects }) => ({
    projects: projects.filter(resource => resource.type === PROJECT),
    tableName: 'archived'
  })),
  withTableColumns({
    tableName: 'archived',
    defaultColumns: [
      COLUMN_KEY.PROJECT_OWNER,
      COLUMN_KEY.PROJECT_GROUP,
      COLUMN_KEY.PRODUCT_BUILD_VERSION,
      COLUMN_KEY.PRODUCT_UPDATED_ON
    ]
  }),
)(Display);
