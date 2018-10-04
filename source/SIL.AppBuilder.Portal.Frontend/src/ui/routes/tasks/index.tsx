import * as React from 'react';
import { compose } from 'recompose';
import { Container } from 'semantic-ui-react';

import { withData, WithDataProps } from 'react-orbitjs';

import { query, TASKS_TYPE } from '@data';
import { withStubbedDevData } from '@data/with-stubbed-dev-data';
import { TaskAttributes, TYPE_NAME as TASKS } from '@data/models/task';
import { withTranslations, i18nProps } from '@lib/i18n';
import { requireAuth } from '@lib/auth';
import { isEmpty } from '@lib/collection';
import { withLayout } from '@ui/components/layout';

import './tasks.scss';
import Row from './row';
import { ResourceObject } from 'jsonapi-typescript';

export const pathName = '/tasks';

export interface IOwnProps {
  tasks: Array<ResourceObject<TASKS_TYPE, TaskAttributes>>;
}

export type IProps =
  & IOwnProps
  & WithDataProps
  & i18nProps;

// TODO: backend endpoint not implement
//       use query / mapNetwork when backend endpoint is implemented
const mapNetworkToProps = {
  tasks: q => q.findRecords(TASKS)
};

const mapRecordsToProps = (ownProps) => {
  return {
    tasks: q => q.findRecords(TASKS)
  };
};

class Tasks extends React.Component<IProps> {
  render() {
    const { tasks, t } = this.props;

    const cellClasses = 'd-xs-none d-md-table-cell';
    const cellSecondaryClasses = 'd-xs-none d-sm-table-cell flex align-items-center';

    return (
      <div className='ui container tasks'>
        <h1 className='page-heading'>{t('tasks.title')}</h1>

        <table className='ui table unstackable'>
          <thead>
            <tr>
              <th>{t('tasks.project')}</th>
              <th className={cellSecondaryClasses}>{t('tasks.product')}</th>
              <th className={cellClasses}>{t('tasks.assignedTo')}</th>
              <th className={cellClasses}>{t('tasks.status')}</th>
              <th className={cellClasses}>{t('tasks.waitTime')}</th>
              <th />
            </tr>
          </thead>
          <tbody>
            { tasks && tasks.map(( task, i ) => (
              <Row key={i}
                cellClasses={cellClasses}
                cellSecondaryClasses={cellSecondaryClasses}
                task={task} />
            )) }

            { isEmpty(tasks) && (
              <tr>
                <td colSpan={6}>
                  <p>{t('tasks.noTasksDescription')}</p>
                </td>
              </tr>
            ) }
          </tbody>
        </table>
      </div>
    );
  }
}


export default compose(
  withLayout,
  requireAuth,
  // query(mapNetworkToProps),
  withData(mapRecordsToProps),
  withTranslations
)(Tasks);
