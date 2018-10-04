import * as React from 'react';
import { compose } from 'recompose';

import { withTableColumns, COLUMN_KEY } from '@ui/components/project-table';

import Table from '@ui/components/project-table/table';

import '@ui/components/project-table/project-table.scss';

export default compose(
  withTableColumns({
    tableName: 'organization',
    defaultColumns: [
      COLUMN_KEY.PROJECT_OWNER,
      COLUMN_KEY.PROJECT_GROUP,
      COLUMN_KEY.PRODUCT_BUILD_VERSION,
      COLUMN_KEY.PRODUCT_UPDATED_ON
    ]
  }),
)(Table);
