import dayjs from 'dayjs'
import {
  Alert,
  App as AntApp,
  Button,
  Card,
  DatePicker,
  Descriptions,
  Drawer,
  Empty,
  Form,
  Input,
  InputNumber,
  Modal,
  Pagination,
  Select,
  Spin,
  Table,
  Tag,
  Typography,
} from 'antd'
import type { TableColumnsType } from 'antd'
import {
  ArrowClockwise,
  CheckCircle,
  Funnel,
  MagnifyingGlass,
  XCircle,
} from '@phosphor-icons/react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import type {
  ApplicationStatus,
  ApproveApplicationRequest,
  EmployeeApplicationListItem,
  EmployeeApplicationSortField,
  EmployeeApplicationsQuery,
  RejectApplicationRequest,
} from '../../api/applications.types'
import { applicationsApi } from '../../api/applicationsApi'
import { getApiErrorMessage } from '../../lib/apiErrorMessage'
import { formatCurrency, formatDate, formatPercent } from '../../lib/formatters'
import AuthNavigation from '../auth/AuthNavigation'
import './employee.css'

const pageSize = 10

const statusLabels: Record<ApplicationStatus, string> = {
  Draft: 'Черновик',
  Submitted: 'Подана',
  Scoring: 'Скоринг',
  AutoApproved: 'Автоодобрена',
  UnderReview: 'На рассмотрении',
  Approved: 'Одобрена',
  Rejected: 'Отклонена',
  Withdrawn: 'Отозвана',
}

const statusColors: Record<ApplicationStatus, string> = {
  Draft: 'default',
  Submitted: 'blue',
  Scoring: 'processing',
  AutoApproved: 'green',
  UnderReview: 'gold',
  Approved: 'success',
  Rejected: 'error',
  Withdrawn: 'default',
}

const statusOptions = Object.entries(statusLabels).map(([value, label]) => ({
  value,
  label,
}))

type FilterFormValues = {
  minAmount?: number
  maxAmount?: number
  createdFrom?: dayjs.Dayjs
  createdTo?: dayjs.Dayjs
  minScore?: number
  maxScore?: number
  status?: ApplicationStatus
  sortBy: EmployeeApplicationSortField
  descending: boolean
}

type FilterValues = Omit<FilterFormValues, 'createdFrom' | 'createdTo'> & {
  createdFrom?: string
  createdTo?: string
}

type DecisionValues = ApproveApplicationRequest & RejectApplicationRequest

function dateFilter(value?: string) {
  return value ? `${value}T00:00:00.000Z` : undefined
}

function statusTag(status: ApplicationStatus) {
  return <Tag color={statusColors[status]}>{statusLabels[status]}</Tag>
}

function EmployeeApplicationDetails({
  applicationId,
  onDecision,
}: {
  applicationId: string
  onDecision: (decision: 'approve' | 'reject') => void
}) {
  const detailsQuery = useQuery({
    queryKey: ['employee-application', applicationId],
    queryFn: ({ signal }) =>
      applicationsApi.getEmployeeById(applicationId, signal),
  })

  if (detailsQuery.isPending) {
    return (
      <div className="employee-detail-loading">
        <Spin size="large" />
        <Typography.Text type="secondary">Загружаем заявку</Typography.Text>
      </div>
    )
  }

  if (detailsQuery.isError || !detailsQuery.data) {
    return (
      <Alert
        type="error"
        showIcon
        message="Не удалось загрузить заявку"
        description={getApiErrorMessage(
          detailsQuery.error,
          'Попробуйте обновить данные.',
        )}
        action={
          <Button size="small" onClick={() => void detailsQuery.refetch()}>
            Повторить
          </Button>
        }
      />
    )
  }

  const { application, proposedSchedule, scoringResults } = detailsQuery.data
  const canApprove =
    application.status === 'UnderReview' ||
    application.status === 'AutoApproved'

  return (
    <div className="employee-detail-content">
      <div className="employee-detail-title">
        <div>
          <Typography.Text className="employee-detail-kicker">
            ЗАЯВКА
          </Typography.Text>
          <Typography.Title level={3}>
            {application.productName}
          </Typography.Title>
        </div>
        {statusTag(application.status)}
      </div>

      <Descriptions bordered size="small" column={{ xs: 1, sm: 2 }}>
        <Descriptions.Item label="Сумма">
          {formatCurrency(application.amount)}
        </Descriptions.Item>
        <Descriptions.Item label="Срок">
          {application.termMonths} мес.
        </Descriptions.Item>
        <Descriptions.Item label="Ставка">
          {formatPercent(application.interestRate)}
        </Descriptions.Item>
        <Descriptions.Item label="Балл скоринга">
          {application.score ?? 'Нет оценки'}
        </Descriptions.Item>
        <Descriptions.Item label="Доход на дату заявки">
          {application.incomeAtApply === null
            ? 'Не указан'
            : formatCurrency(application.incomeAtApply)}
        </Descriptions.Item>
        <Descriptions.Item label="Текущие платежи">
          {application.existingPaymentsAtApply === null
            ? 'Не указаны'
            : formatCurrency(application.existingPaymentsAtApply)}
        </Descriptions.Item>
        <Descriptions.Item label="Возраст на дату заявки">
          {application.ageAtApply === null
            ? 'Не указан'
            : `${application.ageAtApply} лет`}
        </Descriptions.Item>
        <Descriptions.Item label="Подана">
          {formatDate(application.createdAt)}
        </Descriptions.Item>
      </Descriptions>

      <section className="employee-detail-section">
        <div className="employee-section-heading">
          <div>
            <Typography.Text className="employee-detail-kicker">
              ОБЪЯСНИМОЕ РЕШЕНИЕ
            </Typography.Text>
            <Typography.Title level={4}>Результаты скоринга</Typography.Title>
          </div>
          {application.score !== null && (
            <span className="employee-score">
              {application.score}
              <small> / 100</small>
            </span>
          )}
        </div>
        {scoringResults.length === 0 ? (
          <Empty
            image={Empty.PRESENTED_IMAGE_SIMPLE}
            description="Результаты ещё не сформированы"
          />
        ) : (
          <div className="employee-scoring-list">
            {scoringResults.map((result) => (
              <article className="employee-scoring-item" key={result.ruleCode}>
                {result.passed ? (
                  <CheckCircle size={20} weight="regular" aria-hidden="true" />
                ) : (
                  <XCircle size={20} weight="regular" aria-hidden="true" />
                )}
                <div>
                  <strong>{result.ruleName}</strong>
                  <p>{result.details}</p>
                </div>
                <span
                  className={
                    result.points < 0 ? 'employee-points-negative' : ''
                  }
                >
                  {result.points > 0 ? '+' : ''}
                  {result.points}
                </span>
              </article>
            ))}
          </div>
        )}
      </section>

      <section className="employee-detail-section">
        <div className="employee-section-heading">
          <div>
            <Typography.Text className="employee-detail-kicker">
              ПРЕДЛОЖЕНИЕ
            </Typography.Text>
            <Typography.Title level={4}>
              Предварительный график
            </Typography.Title>
          </div>
        </div>
        <div className="employee-schedule-summary">
          <div>
            <span>Первый платёж</span>
            <strong>{formatDate(proposedSchedule.firstPaymentDate)}</strong>
          </div>
          <div>
            <span>Всего выплат</span>
            <strong>
              {formatCurrency(proposedSchedule.schedule.totalPaid)}
            </strong>
          </div>
          <div>
            <span>Переплата</span>
            <strong>
              {formatCurrency(proposedSchedule.schedule.overpayment)}
            </strong>
          </div>
        </div>
        <div className="employee-schedule-table-wrap">
          <table className="employee-schedule-table">
            <thead>
              <tr>
                <th>№</th>
                <th>Дата</th>
                <th>Платёж</th>
                <th>Проценты</th>
                <th>Остаток</th>
              </tr>
            </thead>
            <tbody>
              {proposedSchedule.schedule.payments.map((payment) => (
                <tr key={payment.number}>
                  <td>{payment.number}</td>
                  <td>{formatDate(payment.date)}</td>
                  <td>{formatCurrency(payment.payment)}</td>
                  <td>{formatCurrency(payment.interestPart)}</td>
                  <td>{formatCurrency(payment.remainingBalance)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      {application.statusHistory.length > 0 && (
        <section className="employee-detail-section">
          <div className="employee-section-heading">
            <div>
              <Typography.Text className="employee-detail-kicker">
                ИЗМЕНЕНИЯ
              </Typography.Text>
              <Typography.Title level={4}>История статусов</Typography.Title>
            </div>
          </div>
          <div className="employee-history-list">
            {application.statusHistory.map((entry, index) => (
              <div
                className="employee-history-item"
                key={`${entry.changedAt}-${index}`}
              >
                <span>{statusTag(entry.toStatus)}</span>
                <time>{formatDate(entry.changedAt)}</time>
                {entry.comment && <p>{entry.comment}</p>}
              </div>
            ))}
          </div>
        </section>
      )}

      {canApprove && (
        <div className="employee-decision-actions">
          {application.status === 'UnderReview' && (
            <Button
              danger
              icon={<XCircle size={18} />}
              onClick={() => onDecision('reject')}
            >
              Отклонить
            </Button>
          )}
          <Button
            type="primary"
            icon={<CheckCircle size={18} />}
            onClick={() => onDecision('approve')}
          >
            Одобрить
          </Button>
        </div>
      )}
    </div>
  )
}

export default function EmployeeCabinetPage() {
  const { message } = AntApp.useApp()
  const queryClient = useQueryClient()
  const [filters, setFilters] = useState<FilterValues>({
    sortBy: 'CreatedAt',
    descending: true,
  })
  const [page, setPage] = useState(1)
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [decision, setDecision] = useState<'approve' | 'reject' | null>(null)
  const [form] = Form.useForm<DecisionValues>()

  const query = useMemo<EmployeeApplicationsQuery>(
    () => ({
      page,
      pageSize,
      sortBy: filters.sortBy,
      descending: filters.descending,
      minAmount: filters.minAmount,
      maxAmount: filters.maxAmount,
      createdFrom: dateFilter(filters.createdFrom),
      createdTo: filters.createdTo
        ? `${filters.createdTo}T23:59:59.999Z`
        : undefined,
      minScore: filters.minScore,
      maxScore: filters.maxScore,
      status: filters.status,
    }),
    [filters, page],
  )

  const applicationsQuery = useQuery({
    queryKey: ['employee-applications', query],
    queryFn: ({ signal }) => applicationsApi.getEmployeePage(query, signal),
  })

  const refreshApplications = async () => {
    await queryClient.invalidateQueries({ queryKey: ['employee-applications'] })
    if (selectedId) {
      await queryClient.invalidateQueries({
        queryKey: ['employee-application', selectedId],
      })
    }
  }

  const approveMutation = useMutation({
    mutationFn: (values: ApproveApplicationRequest) =>
      applicationsApi.approveEmployeeApplication(selectedId!, values),
    onSuccess: async () => {
      setDecision(null)
      message.success('Заявка одобрена. График платежей сохранён.')
      await refreshApplications()
    },
    onError: (error) =>
      message.error(
        getApiErrorMessage(
          error,
          'Не удалось одобрить заявку. Обновите карточку и попробуйте снова.',
        ),
      ),
  })

  const rejectMutation = useMutation({
    mutationFn: (values: RejectApplicationRequest) =>
      applicationsApi.rejectEmployeeApplication(selectedId!, values),
    onSuccess: async () => {
      setDecision(null)
      message.success('Заявка отклонена.')
      await refreshApplications()
    },
    onError: (error) =>
      message.error(
        getApiErrorMessage(
          error,
          'Не удалось отклонить заявку. Обновите карточку и попробуйте снова.',
        ),
      ),
  })

  const columns: TableColumnsType<EmployeeApplicationListItem> = [
    {
      title: 'Заявка',
      key: 'application',
      render: (_, item) => (
        <div className="employee-table-product">
          <strong>{item.productName}</strong>
          <span>
            {formatDate(item.createdAt)} · {item.termMonths} мес.
          </span>
        </div>
      ),
    },
    {
      title: 'Сумма',
      dataIndex: 'amount',
      key: 'amount',
      sorter: true,
      render: (amount: number) => (
        <span className="employee-table-number">{formatCurrency(amount)}</span>
      ),
    },
    {
      title: 'Ставка',
      dataIndex: 'interestRate',
      key: 'interestRate',
      render: (rate: number) => formatPercent(rate),
    },
    {
      title: 'Скоринг',
      dataIndex: 'score',
      key: 'score',
      sorter: true,
      render: (score: number | null) =>
        score === null ? (
          <span className="employee-muted">—</span>
        ) : (
          <span className="employee-score-cell">{score}</span>
        ),
    },
    {
      title: 'Статус',
      dataIndex: 'status',
      key: 'status',
      render: (status: ApplicationStatus) => statusTag(status),
    },
    {
      title: '',
      key: 'open',
      align: 'right',
      render: (_, item) => (
        <Button type="link" onClick={() => setSelectedId(item.id)}>
          Открыть
        </Button>
      ),
    },
  ]

  const applyFilters = (values: FilterFormValues) => {
    setFilters({
      ...values,
      createdFrom: values.createdFrom?.format('YYYY-MM-DD'),
      createdTo: values.createdTo?.format('YYYY-MM-DD'),
    })
    setPage(1)
  }

  const openDecision = (nextDecision: 'approve' | 'reject') => {
    if (nextDecision === 'approve' && applicationsQuery.data) {
      const currentItem = applicationsQuery.data.items.find(
        (item) => item.id === selectedId,
      )
      if (currentItem) {
        form.setFieldsValue({
          amount: currentItem.amount,
          termMonths: currentItem.termMonths,
          interestRate: currentItem.interestRate,
          comment: '',
        })
      }
    } else {
      form.resetFields()
    }
    setDecision(nextDecision)
  }

  const submitDecision = async () => {
    try {
      const values = await form.validateFields()
      if (decision === 'approve') {
        await approveMutation.mutateAsync({
          amount: values.amount,
          termMonths: values.termMonths,
          interestRate: values.interestRate,
          comment: values.comment?.trim() || null,
        })
      } else if (decision === 'reject') {
        await rejectMutation.mutateAsync({ comment: values.comment.trim() })
      }
    } catch {
      // Validation errors and API errors are displayed by Ant Design and the mutation handlers.
    }
  }

  const busy = approveMutation.isPending || rejectMutation.isPending

  return (
    <main className="employee-shell">
      <AuthNavigation />
      <header className="employee-page-header">
        <div>
          <p className="eyebrow">РАБОЧАЯ ОБЛАСТЬ · КРЕДИТНЫЙ ОТДЕЛ</p>
          <Typography.Title level={1}>Очередь заявок</Typography.Title>
          <Typography.Paragraph>
            Изучайте результаты скоринга и условия кредита, чтобы принимать
            обоснованные решения.
          </Typography.Paragraph>
        </div>
        <div className="employee-queue-counter">
          <span>Найдено заявок</span>
          <strong>{applicationsQuery.data?.totalCount ?? '—'}</strong>
        </div>
      </header>

      <Card className="employee-filter-card" bordered={false}>
        <div className="employee-card-heading">
          <div className="employee-filter-title">
            <Funnel size={20} weight="regular" aria-hidden="true" />
            <div>
              <strong>Фильтры очереди</strong>
              <span>Сужайте выборку по условиям заявки</span>
            </div>
          </div>
          <Button
            icon={<ArrowClockwise size={17} />}
            onClick={() => {
              applyFilters({ sortBy: 'CreatedAt', descending: true })
              void queryClient.invalidateQueries({
                queryKey: ['employee-applications'],
              })
            }}
          >
            Сбросить
          </Button>
        </div>
        <Form
          className="employee-filter-form"
          layout="vertical"
          initialValues={{
            ...filters,
            createdFrom: filters.createdFrom
              ? dayjs(filters.createdFrom)
              : undefined,
            createdTo: filters.createdTo ? dayjs(filters.createdTo) : undefined,
          }}
          onFinish={applyFilters}
          key={JSON.stringify(filters)}
        >
          <Form.Item label="Статус" name="status">
            <Select
              allowClear
              placeholder="Любой статус"
              options={statusOptions}
            />
          </Form.Item>
          <Form.Item label="Сумма от, BYN" name="minAmount">
            <InputNumber min={0} precision={2} placeholder="Без ограничения" />
          </Form.Item>
          <Form.Item label="Сумма до, BYN" name="maxAmount">
            <InputNumber min={0} precision={2} placeholder="Без ограничения" />
          </Form.Item>
          <Form.Item label="Балл от" name="minScore">
            <InputNumber min={0} max={100} precision={0} placeholder="0" />
          </Form.Item>
          <Form.Item label="Балл до" name="maxScore">
            <InputNumber min={0} max={100} precision={0} placeholder="100" />
          </Form.Item>
          <Form.Item label="Дата от" name="createdFrom">
            <DatePicker className="employee-date-picker" format="DD.MM.YYYY" />
          </Form.Item>
          <Form.Item label="Дата до" name="createdTo">
            <DatePicker className="employee-date-picker" format="DD.MM.YYYY" />
          </Form.Item>
          <Form.Item label="Сортировка" name="sortBy">
            <Select
              options={[
                { value: 'CreatedAt', label: 'Дата подачи' },
                { value: 'Amount', label: 'Сумма' },
                { value: 'Score', label: 'Балл скоринга' },
                { value: 'Status', label: 'Статус' },
              ]}
            />
          </Form.Item>
          <Form.Item label="Порядок" name="descending">
            <Select
              options={[
                { value: true, label: 'Сначала новые / большие' },
                { value: false, label: 'Сначала старые / меньшие' },
              ]}
            />
          </Form.Item>
          <Button
            className="employee-filter-submit"
            htmlType="submit"
            type="primary"
            icon={<MagnifyingGlass size={18} />}
          >
            Применить
          </Button>
        </Form>
      </Card>

      <section className="employee-results" aria-label="Заявки клиентов">
        {applicationsQuery.isError ? (
          <Alert
            type="error"
            showIcon
            message="Не удалось загрузить очередь заявок"
            description={getApiErrorMessage(
              applicationsQuery.error,
              'Проверьте подключение и попробуйте ещё раз.',
            )}
            action={
              <Button onClick={() => void applicationsQuery.refetch()}>
                Повторить
              </Button>
            }
          />
        ) : (
          <>
            <div className="employee-results-heading">
              <div>
                <Typography.Title level={3}>Заявки</Typography.Title>
                <Typography.Text type="secondary">
                  Страница {page} · по {pageSize} записей
                </Typography.Text>
              </div>
              <Button
                icon={<ArrowClockwise size={17} />}
                onClick={() => void applicationsQuery.refetch()}
                loading={applicationsQuery.isFetching}
              >
                Обновить
              </Button>
            </div>
            <div className="employee-table-wrap">
              <Table<EmployeeApplicationListItem>
                rowKey="id"
                columns={columns}
                dataSource={applicationsQuery.data?.items ?? []}
                loading={applicationsQuery.isPending}
                pagination={false}
                onChange={(_, __, sorter) => {
                  const activeSorter = Array.isArray(sorter)
                    ? sorter[0]
                    : sorter
                  if (!activeSorter?.order) return
                  const sortBy: EmployeeApplicationSortField =
                    activeSorter.field === 'amount'
                      ? 'Amount'
                      : activeSorter.field === 'score'
                        ? 'Score'
                        : 'CreatedAt'
                  setFilters({
                    ...filters,
                    sortBy,
                    descending: activeSorter.order === 'descend',
                  })
                  setPage(1)
                }}
                locale={{
                  emptyText: (
                    <Empty description="По заданным условиям заявок нет" />
                  ),
                }}
                scroll={{ x: 680 }}
              />
            </div>
            <div className="employee-pagination">
              <Typography.Text type="secondary">
                {applicationsQuery.data?.totalCount ?? 0} заявок в выборке
              </Typography.Text>
              <Pagination
                current={page}
                pageSize={pageSize}
                total={applicationsQuery.data?.totalCount ?? 0}
                showSizeChanger={false}
                showLessItems
                onChange={setPage}
              />
            </div>
          </>
        )}
      </section>

      <Drawer
        className="employee-drawer"
        title="Карточка заявки"
        open={selectedId !== null}
        onClose={() => setSelectedId(null)}
        width={720}
        destroyOnClose
        footer={null}
      >
        {selectedId && (
          <EmployeeApplicationDetails
            applicationId={selectedId}
            onDecision={openDecision}
          />
        )}
      </Drawer>

      <Modal
        title={
          decision === 'approve' ? 'Условия одобрения' : 'Отклонение заявки'
        }
        open={decision !== null}
        onCancel={() => setDecision(null)}
        onOk={() => void submitDecision()}
        okText={decision === 'approve' ? 'Одобрить заявку' : 'Отклонить заявку'}
        cancelText="Вернуться"
        okButtonProps={{ danger: decision === 'reject', loading: busy }}
        destroyOnClose
      >
        {decision === 'reject' ? (
          <Form form={form} layout="vertical" preserve={false}>
            <Form.Item
              label="Комментарий к отказу"
              name="comment"
              rules={[
                {
                  required: true,
                  whitespace: true,
                  message: 'Укажите причину отказа.',
                },
                {
                  max: 1000,
                  message: 'Комментарий должен быть не длиннее 1000 символов.',
                },
              ]}
            >
              <Input.TextArea
                rows={4}
                maxLength={1000}
                showCount
                placeholder="Опишите причину решения"
              />
            </Form.Item>
          </Form>
        ) : (
          <Form form={form} layout="vertical" preserve={false}>
            <Alert
              className="employee-approval-hint"
              type="info"
              showIcon
              message="Можно скорректировать предложение"
              description="После одобрения по этим условиям для клиента сохранится график платежей."
            />
            <Form.Item
              label="Сумма, BYN"
              name="amount"
              rules={[{ required: true, message: 'Укажите сумму.' }]}
            >
              <InputNumber
                min={0.01}
                precision={2}
                className="employee-modal-input"
              />
            </Form.Item>
            <Form.Item
              label="Срок, месяцев"
              name="termMonths"
              rules={[{ required: true, message: 'Укажите срок.' }]}
            >
              <InputNumber
                min={1}
                max={360}
                precision={0}
                className="employee-modal-input"
              />
            </Form.Item>
            <Form.Item
              label="Ставка, % годовых"
              name="interestRate"
              rules={[{ required: true, message: 'Укажите ставку.' }]}
            >
              <InputNumber
                min={0}
                max={60}
                precision={2}
                className="employee-modal-input"
              />
            </Form.Item>
            <Form.Item
              label="Комментарий клиенту (необязательно)"
              name="comment"
              rules={[
                {
                  max: 1000,
                  message: 'Комментарий должен быть не длиннее 1000 символов.',
                },
              ]}
            >
              <Input.TextArea
                rows={3}
                maxLength={1000}
                showCount
                placeholder="Например, условия одобрения"
              />
            </Form.Item>
          </Form>
        )}
      </Modal>
    </main>
  )
}
